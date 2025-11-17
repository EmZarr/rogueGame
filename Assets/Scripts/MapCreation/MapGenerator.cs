using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.Rendering.DebugUI;

public class MapGenerator : MonoBehaviour
{
    [Header("Geometry")]
    [SerializeField] Vector3Int mapSize;
    [SerializeField] Vector3Int maxOutLineSize;

    [SerializeField] int maxAmountOfOutlines;
    [SerializeField] Vector3Int maxRoomSize;
    [SerializeField] int maxRoomAmount;

    [Header("Enemies")]
    [SerializeField] int amountOfEnemyTypes;
    [SerializeField] int startingBudget;
    [SerializeField] float minDistanceToPlayer;
    [SerializeField] float minDistanceBetweenEnemies;


    [Header("Furnishing")]
    [SerializeField] int maxAmountFurnishing;
    [SerializeField] int amountOfFurnishingTypes;


    [Header("Controls")]
    public InputActionReference placeObjects;   
    public InputActionReference remakeMap;
    public InputActionReference mutateMap;
    public InputActionReference mutatePlacements;
    public InputActionReference mutateContent;

    public int[,] mapArray;
    private List<(Vector3Int placement, Vector3Int size)> rooms;

    private List<(Vector3Int placement, Vector3Int size)> outlines;

    private List<(Vector2Int placement, int type)> enemies;

    private List<(Vector2Int placement, int type)> furnishing;
    private Vector2Int playerStartPos;
    private Vector3Int outlinePlacement;
    private Vector3Int outlineSize;
    private int budget;

    private Vector2Int playerTest;

    private bool[,] keep;

    private List<(Vector2Int start, Vector2Int end)> componentConnections;

    private int furnishingBudget;

    MapInstantiator mapInstantiator;

    void Awake()
    {
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
    }
    void Start()
    {
        enemies = new List<(Vector2Int, int)>();
        furnishing = new List<(Vector2Int, int)>();
        budget = startingBudget;
        furnishing = new List<(Vector2Int, int)>();
        furnishingBudget = maxAmountFurnishing;
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
        RemakeMap();
    }

    void OnEnable()
    {
        placeObjects.action.Enable();
        remakeMap.action.Enable();
        mutateMap.action.Enable();
        mutatePlacements.action.Enable();
        mutateContent.action.Enable();
        placeObjects.action.performed += ctx => PlaceObjects();
        remakeMap.action.performed += ctx => RemakeMap();
        mutateMap.action.performed += ctx => MutateMap();
        mutatePlacements.action.performed += ctx => MutatePlacements();
        mutateContent.action.performed += ctx => MutateContent();
    }

    private void PlaceObjects()
    {
        placeFurnishing();
        placeEnemies();
        mapInstantiator.makeMap(mapArray, playerTest);
    }

    private void RemakeMap()
    {
        enemies = new List<(Vector2Int, int)>();
        new List<(Vector2Int, int)>();
        budget = startingBudget;
        furnishingBudget = maxAmountFurnishing;
        makeRoomGeometry();
        Debug.Log("PlayerPosVal: " + mapArray[playerTest.x, playerTest.y]);
        mapInstantiator.makeMap(mapArray, playerTest);
    }

    private void MutateMap()
    {
        mutateGeometry();
        mapInstantiator.makeMap(mapArray, playerTest);
    }

    private void MutateContent()
    {
        mutateFurnishing();
        mapInstantiator.makeMap(mapArray, playerTest);
    }

    private void MutatePlacements()
    {
        mutateEnemies();
        mapInstantiator.makeMap(mapArray, playerTest);
    }


    void makeRoomGeometry()
    {
        mapArray = new int[mapSize.x, mapSize.y];
        rooms = new List<(Vector3Int, Vector3Int)>();
        outlines = new List<(Vector3Int, Vector3Int)>();
        // outline size

        int amountOfOutlines = Random.Range(1, maxAmountOfOutlines + 1);

        for (int o = 0; o < amountOfOutlines; o++)
        {
            outlineSize = new Vector3Int(
                Random.Range(5, maxOutLineSize.x),
                Random.Range(5, maxOutLineSize.y),
                0
            );
            // outline placement (make sure it fits)

            outlinePlacement = new Vector3Int(
                Random.Range(0, mapSize.x - outlineSize.x + 1),
                Random.Range(0, mapSize.y - outlineSize.y + 1),
                0
            );

            outlines.Add((outlinePlacement, outlineSize));

            int roomAmount = Random.Range(1, maxRoomAmount);
            for (int i = 0; i < roomAmount; i++)
            {
                // clamp room size so it can't be bigger than the outline
                int roomW = Random.Range(2, Mathf.Min(maxRoomSize.x, outlineSize.x) + 1);
                int roomH = Random.Range(2, Mathf.Min(maxRoomSize.y, outlineSize.y) + 1);
                Vector3Int roomSize = new Vector3Int(roomW, roomH, 0);

                // place room inside outline    
                Vector3Int roomPlacement = new Vector3Int(
                    Random.Range(outlinePlacement.x, outlinePlacement.x + outlineSize.x - roomSize.x + 1),
                    Random.Range(outlinePlacement.y, outlinePlacement.y + outlineSize.y - roomSize.y + 1),
                    0
                );

                rooms.Add((roomPlacement, roomSize));
            }

        }
        
        // See outline as walls
        /*
        for (int x = outlinePlacement.x; x < outlinePlacement.x + outlineSize.x; x++)
        {
            for (int y = outlinePlacement.y; y < outlinePlacement.y + outlineSize.y; y++)
            {
                mapArray[x, y] = 2;
            }
        }*/

        // paint
        foreach (var room in rooms)
        {
            for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
            {
                for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                {
                    mapArray[x, y] = 1;
                }
            }
        }

        RemoveDisconnectedFloors();
        AddWallsAroundFloors();
    }


    void RemoveDisconnectedFloors()
    {
        // this will remember which tiles we have visted
        List<List<Vector2Int>> components = new List<List<Vector2Int>>();
        componentConnections = new List<(Vector2Int, Vector2Int)>();
        bool[,] visited = new bool[mapSize.x, mapSize.y];

        // this will remember which tiles belong to the largest component
        keep = new bool[mapSize.x, mapSize.y];
        bool playerPlaced = false;

        int largestSize = 0;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                // start a new component if we find an unvisited floor
                if (mapArray[x, y] == 1 && !visited[x, y])
                {
                    // BFS for this component
                    Queue<Vector2Int> q = new Queue<Vector2Int
                        >();
                    List<Vector2Int> thisComponentTiles = new List<Vector2Int>();
                    List<Vector2Int> edgeTiles = new List<Vector2Int>();

                    q.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    // While there are still tiles in the queue to explore
                    while (q.Count > 0)
                    {
                        // Take (dequeue) the next tile position to check
                        var cur = q.Dequeue();

                        // Add it to this component's list � it�s part of the connected floor area we�re exploring
                        thisComponentTiles.Add(cur);

                        // Loop over all 4 directions: right, left, up, down
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = cur.x + dx[i]; // new x position (neighbor)
                            int ny = cur.y + dy[i]; // new y position (neighbor)

                            // Skip this neighbor if it's outside the map boundaries
                            if (nx < 0 || ny < 0 || nx >= mapSize.x || ny >= mapSize.y)
                                continue;

                            // Skip if we�ve already checked this tile before
                            if (visited[nx, ny])
                                continue;

                            // Add floor edge tiles to handle teleporters.
                            if (mapArray[nx, ny] == 0)
                            {
                                edgeTiles.Add(cur);
                            }
                            // If this neighbor is a floor tile (not a wall), it�s part of the same region
                            if (mapArray[nx, ny] == 1)
                            {
                                // Mark it visited so we don�t check it again later
                                visited[nx, ny] = true;

                                // Add this neighbor to the queue so we�ll explore its neighbors next
                                q.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                    // is this the biggest so far?
                    if (thisComponentTiles.Count > largestSize)
                    {
                        largestSize = thisComponentTiles.Count;

                        // reset keep map and mark this component as the one to keep
                        keep = new bool[mapSize.x, mapSize.y];
                        foreach (var tile in thisComponentTiles)
                        {
                            keep[tile.x, tile.y] = true;

                        }
                    }
                    components.Add(edgeTiles);
                }
            }
        }

        /*Debug.Log(playerPlaced);
        while (!playerPlaced) // Put player in first available floor tile
        {
            Debug.Log("Hello?");
            int playerPosSelect = mapArray[Random.Range(0, keep.GetLength(0)), Random.Range(0, keep.GetLength(1))];
            Vector2Int playerPos = components[0][playerPosSelect];
            if (mapArray[playerPos.x, playerPos.y] == 1)
            {
                playerPlaced = true;
                Debug.Log("Here xd");
                mapArray[playerPos.x, playerPos.y] = 100;
                playerTest = new Vector2Int(playerPos.x, playerPos.y);
                Debug.Log("Testing: " + mapArray[playerPos.x, playerPos.y]);
                Debug.Log("Player Pos: " + playerPos);
                playerStartPos = new Vector2Int(playerPos.x, playerPos.y);
            }
        }
        */
        // now remove all floor tiles that are not in the largest component
        /*for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (mapArray[x, y] == 1 && !keep[x, y])
                {
                    mapArray[x, y] = 0;
                }
                if (!playerPlaced && keep[x, y]) // Put player in first available floor tile
                {
                    playerPlaced = true;
                    mapArray[x, y] = 100;
                    playerStartPos = new Vector2Int(x, y);
                }
            }
        }*/
        foreach (var component in components)
        {
            //Choose random starting point in edge tiles for a component
            int randStart = Random.Range(0, component.Count - 1);
            Vector2Int startingPoint = component[randStart];

            //Select random component to be end point
            int randEnd = Random.Range(0, components.Count - 1);
            List<Vector2Int> endComponent = components[randEnd];
            randEnd = Random.Range(0, endComponent.Count);
            Vector2Int endPoint = endComponent[randEnd];

            componentConnections.Add((startingPoint, endPoint));

        }

        ConnectComponents();

    }


    void ConnectComponents()
    {
        foreach (var component in componentConnections)
        {
            if (component.start.x < component.end.x)
            {
                for (int x = component.start.x; x < component.end.x; x++)
                {
                    mapArray[x, component.start.y] = 1;
                    mapArray[x + 1, component.start.y] = 1;
                }
                if (component.start.y <= component.end.y)
                {
                    for (int y = component.start.y; y < component.end.y; y++)
                    {
                        mapArray[component.end.x, y] = 1;
                        mapArray[component.end.x + 1, y] = 1;
                    }
                }
                else
                {
                    for (int y = component.start.y; y > component.end.y; y--)
                    {
                        mapArray[component.end.x, y] = 1;
                        mapArray[component.end.x + 1, y] = 1;
                    }
                }
            }
            if (component.start.x > component.end.x)
            {
                for (int x = component.start.x; x > component.end.x; x--)
                {
                    mapArray[x, component.start.y] = 1;
                    mapArray[x + 1, component.start.y] = 1;
                }
                if (component.start.y <= component.end.y)
                {
                    for (int y = component.start.y; y < component.end.y; y++)
                    {
                        mapArray[component.end.x, y] = 1;
                        mapArray[component.end.x + 1, y] = 1;
                    }
                }
                else
                {
                    for (int y = component.start.y; y > component.end.y; y--)
                    {
                        mapArray[component.end.x, y] = 1;
                        mapArray[component.end.x + 1, y] = 1;
                    }
                }

            }
        }
        bool playerPlaced = false;
        while (!playerPlaced) // Put player in first available floor tile
        {
            Debug.Log("Hello?");
            int playerPosX = Random.Range(0, keep.GetLength(0));
            int playerPosY = Random.Range(0, keep.GetLength(1));

            int playerPosSelect = mapArray[Random.Range(0, keep.GetLength(0)), Random.Range(0, keep.GetLength(1))];
            if (mapArray[playerPosX, playerPosY] == 1)
            {
                playerPlaced = true;
                Debug.Log("Here xd");
                mapArray[playerPosX, playerPosY] = 100;
                playerTest = new Vector2Int(playerPosX, playerPosY);
                playerStartPos = new Vector2Int(playerPosX, playerPosY);
            }
        }
    }

    // Go through tiles, all floors we add walls to empty neighbors
    void AddWallsAroundFloors()
    {
        List<Vector2Int> wallsToPlace = new List<Vector2Int>();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (mapArray[x, y] == 1) // floor
                {
                    TryMarkWall(x + 1, y, wallsToPlace);
                    TryMarkWall(x - 1, y, wallsToPlace);
                    TryMarkWall(x, y + 1, wallsToPlace);
                    TryMarkWall(x, y - 1, wallsToPlace);
                }
            }
        }

        // now actually place them
        foreach (var pos in wallsToPlace)
        {
            mapArray[pos.x, pos.y] = 2;
        }
    }



    void TryMarkWall(int x, int y, List<Vector2Int> walls)
    {
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
            return;

        if (mapArray[x, y] == 0)
            walls.Add(new Vector2Int(x, y));
    }


    void placeEnemies()
    {
        // collect candidate floor tiles
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (mapArray[x, y] == 1) // floor
                {
                    // skip near player
                    if (Vector2Int.Distance(new Vector2Int(x, y), playerStartPos) < minDistanceToPlayer)
                        continue;

                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        // shuffle candidates
        for (int i = 0; i < candidates.Count; i++)
        {
            int swapIndex = Random.Range(i, candidates.Count);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        // place enemies not too close to each other
        foreach (var pos in candidates)
        {
            if (budget <= 0)
                break;

            // check distance to other enemies
            bool tooClose = false;
            foreach (var (p, t) in enemies)
            {
                if (Vector2Int.Distance(pos, p) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;

            // place enemy
            int enemyType = Random.Range(0, amountOfEnemyTypes);
            enemies.Add((pos, enemyType));
            budget--;
        }

        foreach (var (p, t) in enemies)
        {
            mapArray[p.x, p.y] = 6 + t;
        }
    }

    // Remove enemies that are no longer on floor tiles
    void ValidateEnemiesAgainstMap()
    {
        int oldCount = enemies.Count;
        var valid = new List<(Vector2Int placement, int type)>();

        foreach (var (pos, type) in enemies)
        {
            bool inside =
                pos.x >= 0 && pos.x < mapSize.x &&
                pos.y >= 0 && pos.y < mapSize.y;

            if (inside && mapArray[pos.x, pos.y] == 1) // still floor
            {
                valid.Add((pos, type));
            }
        }

        enemies = valid;

        int removed = oldCount - enemies.Count;
        budget += removed;
    }


    void placeFurnishing()
    {
        List<Vector2Int> placedFurnishingPositions = new List<Vector2Int>();
        int iterations = 0; // for safety to avoid infinite loops
        while (furnishingBudget > 0 && iterations < 1000)
        {
            iterations++;
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    if (furnishingBudget <= 0)
                        break;
                    if (mapArray[x, y] == 1) // empty floor
                    {
                        // check distance to player and other enemies
                        if (Vector2Int.Distance(new Vector2Int(x, y), playerStartPos) < minDistanceToPlayer)
                            continue;
                        bool tooCloseToOther = false;
                        // if valid, place enemy and reduce budget
                        int placeFurnishing = Random.Range(0, 100);
                        if (placeFurnishing < 1) // 1% chance to place an enemy
                        {
                            placedFurnishingPositions.Add(new Vector2Int(x, y));
                            int furnishType = Random.Range(0, amountOfFurnishingTypes);
                            mapArray[x, y] = 3 + furnishType;
                            furnishing.Add((new Vector2Int(x, y), 3 + furnishType));
                            furnishingBudget -= 1;
                        }
                    }
                }
            }
        }
    }

    void mutateGeometry()
    {

        // Remove a random existing room
        int removeIndex = Random.Range(0, rooms.Count);
        rooms.RemoveAt(removeIndex);

        // Add new room
        int roomW = Random.Range(2, Mathf.Min(maxRoomSize.x, outlineSize.x) + 1);
        int roomH = Random.Range(2, Mathf.Min(maxRoomSize.y, outlineSize.y) + 1);
        Vector3Int roomSize = new Vector3Int(roomW, roomH, 0);  
        Vector3Int roomPlacement = new Vector3Int(
            Random.Range(outlinePlacement.x, outlinePlacement.x + outlineSize.x - roomSize.x + 1),
            Random.Range(outlinePlacement.y, outlinePlacement.y + outlineSize.y - roomSize.y + 1),
            0
        );

        rooms.Add((roomPlacement, roomSize));

        // Clear the map
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                mapArray[x, y] = 0;
            }
        }

        // Repaint rooms
        foreach (var room in rooms)
        {
            for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
            {
                for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                {
                    mapArray[x, y] = 1;
                }
            }
        }
        RemoveDisconnectedFloors();
        AddWallsAroundFloors();

        // Remove enemies that are no longer valid, and place new ones
        ValidateEnemiesAgainstMap();
        placeEnemies();
    }

    void mutateEnemies()
    {
        // no enemies to mutate
        if (enemies == null || enemies.Count == 0)
            return;

        // pick one to remove
        int idx = Random.Range(0, enemies.Count);
        var (pos, type) = enemies[idx];
        enemies.RemoveAt(idx);
        budget++;

        // clear from map
        mapArray[pos.x, pos.y] = 1;

        // add replacement
        placeEnemies();
    }


    void mutateFurnishing()
    {
        // no furnishing to mutate
        if (furnishing == null || furnishing.Count == 0)
            return;

        // pick one to remove
        int idx = Random.Range(0, furnishing.Count);
        var (pos, type) = furnishing[idx];
        furnishing.RemoveAt(idx);
        furnishingBudget++;

        // clear from map
        mapArray[pos.x, pos.y] = 1;

        // add replacement
        placeFurnishing();
    }


    void ValidateFurnishingAgainstMap()
    {
        int oldCount = furnishing.Count;
        var valid = new List<(Vector2Int placement, int type)>();

        foreach (var (pos, type) in furnishing)
        {
            bool inside =
                pos.x >= 0 && pos.x < mapSize.x &&
                pos.y >= 0 && pos.y < mapSize.y;

            if (inside && mapArray[pos.x, pos.y] == 1) // still floor
            {
                valid.Add((pos, type));
            }
        }

        furnishing = valid;

        int removed = oldCount - furnishing.Count;
        furnishingBudget += removed;
    }

}
