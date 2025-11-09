using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.Rendering.DebugUI;

public class MapGenerator : MonoBehaviour
{
    [Header("Geometry")]
    [SerializeField] Vector3Int mapSize;
    [SerializeField] Vector3Int maxOutLineSize;
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

    public int[,] mapArray;
    private List<(Vector3Int placement, Vector3Int size)> rooms;
    private List<(Vector2Int placement, int type)> enemies;
    private Vector2Int playerStartPos;
    private Vector3Int outlinePlacement;
    private Vector3Int outlineSize;
    private int budget;

    MapInstantiator mapInstantiator;


    void Start()
    {
        enemies = new List<(Vector2Int, int)>();
        budget = startingBudget;
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
        RemakeMap();
    }

    void OnEnable()
    {
        placeObjects.action.Enable();
        remakeMap.action.Enable();
        mutateMap.action.Enable();
        mutatePlacements.action.Enable();
        placeObjects.action.performed += ctx => PlaceObjects();
        remakeMap.action.performed += ctx => RemakeMap();
        mutateMap.action.performed += ctx => MutateMap();
        mutatePlacements.action.performed += ctx => MutatePlacements();
    }

    private void PlaceObjects()
    {
        placeFurnishing();
        placeEnemies();
        mapInstantiator.makeMap(mapArray);
    }

    private void RemakeMap()
    {
        enemies = new List<(Vector2Int, int)>();
        budget = startingBudget;
        makeRoomGeometry();
        mapInstantiator.makeMap(mapArray);
    }

    private void MutateMap()
    {
        mutateGeometry();
        mapInstantiator.makeMap(mapArray);
    }

    private void MutatePlacements()
    {
        mutateEnemies();
        mapInstantiator.makeMap(mapArray);
    }


    void makeRoomGeometry()
    {
        mapArray = new int[mapSize.x, mapSize.y];
        rooms = new List<(Vector3Int, Vector3Int)>();
        // outline size
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
        bool[,] visited = new bool[mapSize.x, mapSize.y];

        // this will remember which tiles belong to the largest component
        bool[,] keep = new bool[mapSize.x, mapSize.y];

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
                }
            }
        }
        bool playerPlaced = false;
        // now remove all floor tiles that are not in the largest component
        for (int x = 0; x < mapSize.x; x++)
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
        while (maxAmountFurnishing > 0 && iterations < 1000)
        {
            iterations++;
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    if (maxAmountFurnishing <= 0)
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
                            maxAmountFurnishing -= 1;
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



}
