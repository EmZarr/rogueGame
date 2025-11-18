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

    private bool[,] keep;

    private List<(Vector2Int start, Vector2Int end)> componentConnections;

    private int furnishingBudget;

    public MapInfo currentMap;

    MapInstantiator mapInstantiator;

    void Awake()
    {
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
    }
    void Start()
    {
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
        placeObjects.action.performed += ctx => PlaceObjects(currentMap);
        remakeMap.action.performed += ctx => RemakeMap();
        mutateMap.action.performed += ctx => MutateMap(currentMap);
        mutatePlacements.action.performed += ctx => MutatePlacements(currentMap);
        mutateContent.action.performed += ctx => MutateContent(currentMap);
    }

    private void PlaceObjects(MapInfo map)
    {
        placeFurnishing(map);
        placeEnemies(map);
        mapInstantiator.makeMap(map.mapArray);
    }

    public void RemakeMap()
    {
        currentMap = new MapInfo
        {
            enemies = new List<(Vector2Int, int)>(),
            furnishing = new List<(Vector2Int, int)>(),
            budget = startingBudget,
            furnishingBudget = maxAmountFurnishing,
            exitPos = new Vector2Int(-1, -1) //apperently it is important taht it is -1 -1???
        };
        currentMap = makeRoomGeometry(currentMap);
        mapInstantiator.makeMap(currentMap.mapArray);
    }

    public MapInfo MutateMap(MapInfo map)
    {
        map = mutateGeometry(map);
        mapInstantiator.makeMap(map.mapArray);
        return map;
    }

    public MapInfo MutateContent(MapInfo map)
    {
        map = mutateFurnishing(map);
        mapInstantiator.makeMap(map.mapArray);
        return map;
    }

    public MapInfo MutatePlacements(MapInfo map)
    {
        map = mutateEnemies(map);
        mapInstantiator.makeMap(map.mapArray);
        return map;
    }


    MapInfo makeRoomGeometry(MapInfo map)
    {
        map.mapArray = new int[mapSize.x, mapSize.y];
        map.rooms = new List<(Vector3Int, Vector3Int)>();
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

                map.rooms.Add((roomPlacement, roomSize));
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
        foreach (var room in map.rooms)
        {
            for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
            {
                for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                {
                    map.mapArray[x, y] = 1;
                }
            }
        }

        map = RemoveDisconnectedFloors(map);
        AddWallsAroundFloors(map);
        map = PlaceExit(map);
        return map;
    }


    MapInfo RemoveDisconnectedFloors(MapInfo map)
    {
        // this will remember which tiles we have visted
        List<List<Vector2Int>> components = new List<List<Vector2Int>>();
        map.componentConnections = new List<(Vector2Int, Vector2Int)>();
        bool[,] visited = new bool[mapSize.x, mapSize.y];
        map.walkableTiles = 0;
        map.WallTiles = 0;

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
                if (map.mapArray[x, y] == 1 && !visited[x, y])
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
                        map.walkableTiles++;

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
                            if (map.mapArray[nx, ny] == 0)
                            {
                                edgeTiles.Add(cur);
                            }
                            // If this neighbor is a floor tile (not a wall), it�s part of the same region
                            if (map.mapArray[nx, ny] == 1)
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

            map.componentConnections.Add((startingPoint, endPoint));

        }

        map = ConnectComponents(map);
        return map;

    }


    MapInfo ConnectComponents(MapInfo map)
    {
        foreach (var component in map.componentConnections)
        {
            if (component.start.x < component.end.x)
            {
                for (int x = component.start.x; x < component.end.x; x++)
                {
                    map.mapArray[x, component.start.y] = 1;
                    map.mapArray[x + 1, component.start.y] = 1;
                    map.walkableTiles += 2;
                }
                if (component.start.y <= component.end.y)
                {
                    for (int y = component.start.y; y < component.end.y; y++)
                    {
                        map.mapArray[component.end.x, y] = 1;
                        map.mapArray[component.end.x + 1, y] = 1;
                        map.walkableTiles += 2;
                    }
                }
                else
                {
                    for (int y = component.start.y; y > component.end.y; y--)
                    {
                        map.mapArray[component.end.x, y] = 1;
                        map.mapArray[component.end.x + 1, y] = 1;
                        map.walkableTiles += 2;
                    }
                }
            }
            if (component.start.x > component.end.x)
            {
                for (int x = component.start.x; x > component.end.x; x--)
                {
                    map.mapArray[x, component.start.y] = 1;
                    map.mapArray[x + 1, component.start.y] = 1;
                    map.walkableTiles += 2;
                }
                if (component.start.y <= component.end.y)
                {
                    for (int y = component.start.y; y < component.end.y; y++)
                    {
                        map.mapArray[component.end.x, y] = 1;
                        map.mapArray[component.end.x + 1, y] = 1;
                        map.walkableTiles += 2;
                    }
                }
                else
                {
                    for (int y = component.start.y; y > component.end.y; y--)
                    {
                        map.mapArray[component.end.x, y] = 1;
                        map.mapArray[component.end.x + 1, y] = 1;
                        map.walkableTiles += 2;
                    }
                }

            }
        }
        bool playerPlaced = false;
        while (!playerPlaced) // Put player in first available floor tile
        {
            int playerPosX = Random.Range(0, keep.GetLength(0));
            int playerPosY = Random.Range(0, keep.GetLength(1));

            int playerPosSelect = map.mapArray[Random.Range(0, keep.GetLength(0)), Random.Range(0, keep.GetLength(1))];
            if (map.mapArray[playerPosX, playerPosY] == 1)
            {
                playerPlaced = true;
                map.mapArray[playerPosX, playerPosY] = 100;
                map.playerStartPos = new Vector2Int(playerPosX, playerPosY);
            }
        }

        return map;
    }

    // place exit
    MapInfo PlaceExit(MapInfo map)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int playerRoomIndex = -1;
        for (int i = 0; i < map.rooms.Count; i++)
        {
            var r = map.rooms[i];
            if (map.playerStartPos.x >= r.placement.x &&
                map.playerStartPos.x < r.placement.x + r.size.x &&
                map.playerStartPos.y >= r.placement.y &&
                map.playerStartPos.y < r.placement.y + r.size.y)
            {
                playerRoomIndex = i;
                break;
            }
        }

        int GetRoomIndexForFloor(int fx, int fy)
        {
            for (int i = 0; i < map.rooms.Count; i++)
            {
                var r = map.rooms[i];
                if (fx >= r.placement.x &&
                    fx < r.placement.x + r.size.x &&
                    fy >= r.placement.y &&
                    fy < r.placement.y + r.size.y)
                    return i;
            }
            return -1;
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map.mapArray[x, y] != 2) continue; // only walls

                var neighbours = new Vector2Int[] {
                new Vector2Int(x + 1, y),
                new Vector2Int(x - 1, y),
                new Vector2Int(x, y + 1),
                new Vector2Int(x, y - 1)
            };

                foreach (var n in neighbours)
                {
                    if (n.x < 0 || n.y < 0 || n.x >= mapSize.x || n.y >= mapSize.y) continue;
                    if (map.mapArray[n.x, n.y] != 1) continue; // must border a floor

                    int roomIndex = GetRoomIndexForFloor(n.x, n.y);
                    if (roomIndex != -1 && roomIndex != playerRoomIndex)
                    {
                        candidates.Add(new Vector2Int(x, y));
                        break;
                    }
                }
            }
        }

        var chosen = candidates[Random.Range(0, candidates.Count)];
        map.mapArray[chosen.x, chosen.y] = 8; // exit code
        map.exitPos = chosen;
        return map;
    }


    // Go through tiles, all floors we add walls to empty neighbors
    MapInfo AddWallsAroundFloors(MapInfo map)
    {
        List<Vector2Int> wallsToPlace = new List<Vector2Int>();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map.mapArray[x, y] == 1) // floor
                {
                    map = TryMarkWall(x + 1, y, wallsToPlace, map);
                    map = TryMarkWall(x - 1, y, wallsToPlace, map);
                    map = TryMarkWall(x, y + 1, wallsToPlace, map);
                    map = TryMarkWall(x, y - 1, wallsToPlace, map);
                }
            }
        }

        // now actually place them
        foreach (var pos in wallsToPlace)
        {
            map.mapArray[pos.x, pos.y] = 2;
            map.WallTiles++;
        }
        return map;
    }



    MapInfo TryMarkWall(int x, int y, List<Vector2Int> walls, MapInfo map)
    {
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
            return map;

        if (map.mapArray[x, y] == 0)
            walls.Add(new Vector2Int(x, y));
        return map;
    }


    MapInfo placeEnemies(MapInfo map)
    {
        // collect candidate floor tiles
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map.mapArray[x, y] == 1) // floor
                {
                    // skip near player
                    if (Vector2Int.Distance(new Vector2Int(x, y), map.playerStartPos) < minDistanceToPlayer)
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
            if (map.budget <= 0)
                break;

            // check distance to other enemies
            bool tooClose = false;
            foreach (var (p, t) in map.enemies)
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
            map.enemies.Add((pos, enemyType));
            map.budget--;
        }

        foreach (var (p, t) in map.enemies)
        {
            map.mapArray[p.x, p.y] = 6 + t;
        }

        return map;
    }

    // Remove enemies that are no longer on floor tiles
    MapInfo ValidateEnemiesAgainstMap(MapInfo map)
    {
        int oldCount = map.enemies.Count;
        var valid = new List<(Vector2Int placement, int type)>();

        foreach (var (pos, type) in map.enemies)
        {
            bool inside =
                pos.x >= 0 && pos.x < mapSize.x &&
                pos.y >= 0 && pos.y < mapSize.y;

            if (inside && map.mapArray[pos.x, pos.y] == 1) // still floor
            {
                valid.Add((pos, type));
            }
        }

        map.enemies = valid;

        int removed = oldCount - map.enemies.Count;
        map.budget += removed;
        return map;
    }


    MapInfo placeFurnishing(MapInfo map)
    {
        List<Vector2Int> placedFurnishingPositions = new List<Vector2Int>();
        int iterations = 0; // for safety to avoid infinite loops
        while (map.furnishingBudget > 0 && iterations < 1000)
        {
            iterations++;
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    if (map.furnishingBudget <= 0)
                        break;
                    if (map.mapArray[x, y] == 1) // empty floor
                    {
                        // check distance to player and other enemies
                        if (Vector2Int.Distance(new Vector2Int(x, y), map.playerStartPos) < minDistanceToPlayer)
                            continue;
                        bool tooCloseToOther = false;
                        // if valid, place enemy and reduce budget
                        int placeFurnishing = Random.Range(0, 100);
                        if (placeFurnishing < 1) // 1% chance to place an enemy
                        {
                            placedFurnishingPositions.Add(new Vector2Int(x, y));
                            int furnishType = Random.Range(0, amountOfFurnishingTypes);
                            map.mapArray[x, y] = 3 + furnishType;
                            map.furnishing.Add((new Vector2Int(x, y), 3 + furnishType));
                            map.furnishingBudget -= 1;
                        }
                    }
                }
            }
        }
        return map;
    }

    MapInfo mutateGeometry(MapInfo map)
    {

        // Remove a random existing room
        int removeIndex = Random.Range(0, map.rooms.Count);
        map.rooms.RemoveAt(removeIndex);

        // Add new room
        int roomW = Random.Range(2, Mathf.Min(maxRoomSize.x, outlineSize.x) + 1);
        int roomH = Random.Range(2, Mathf.Min(maxRoomSize.y, outlineSize.y) + 1);
        Vector3Int roomSize = new Vector3Int(roomW, roomH, 0);  
        Vector3Int roomPlacement = new Vector3Int(
            Random.Range(outlinePlacement.x, outlinePlacement.x + outlineSize.x - roomSize.x + 1),
            Random.Range(outlinePlacement.y, outlinePlacement.y + outlineSize.y - roomSize.y + 1),
            0
        );

        map.rooms.Add((roomPlacement, roomSize));

        // Clear the map
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                map.mapArray[x, y] = 0;
            }
        }

        // Repaint rooms
        foreach (var room in map.rooms)
        {
            for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
            {
                for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                {
                    map.mapArray[x, y] = 1;
                }
            }
        }
        map = RemoveDisconnectedFloors(map);
        map = AddWallsAroundFloors(map);
        map = PlaceExit(map);

        // Remove enemies that are no longer valid, and place new ones
        map = ValidateEnemiesAgainstMap(map);
        map = placeEnemies(map);
        return map;
    }

    MapInfo mutateEnemies(MapInfo map)
    {
        // no enemies to mutate
        if (map.enemies == null || map.enemies.Count == 0)
            return map;

        // pick one to remove
        int idx = Random.Range(0, map.enemies.Count);
        var (pos, type) = map.enemies[idx];
        map.enemies.RemoveAt(idx);
        map.budget++;

        // clear from map
        map.mapArray[pos.x, pos.y] = 1;

        // add replacement
        map = placeEnemies(map);
        return map;
    }


    MapInfo mutateFurnishing(MapInfo map)
    {
        // no furnishing to mutate
        if (map.furnishing == null || map.furnishing.Count == 0)
            return map;

        // pick one to remove
        int idx = Random.Range(0, map.furnishing.Count);
        var (pos, type) = map.furnishing[idx];
        map.furnishing.RemoveAt(idx);
        map.furnishingBudget++;

        // clear from map
        map.mapArray[pos.x, pos.y] = 1;

        // add replacement
        map = placeFurnishing(map);
        return map;
    }


    MapInfo ValidateFurnishingAgainstMap(MapInfo map)
    {
        int oldCount = map.furnishing.Count;
        var valid = new List<(Vector2Int placement, int type)>();

        foreach (var (pos, type) in map.furnishing)
        {
            bool inside =
                pos.x >= 0 && pos.x < mapSize.x &&
                pos.y >= 0 && pos.y < mapSize.y;

            if (inside && map.mapArray[pos.x, pos.y] == 1) // still floor
            {
                valid.Add((pos, type));
            }
        }

        map.furnishing = valid;

        int removed = oldCount - furnishing.Count;
        map.furnishingBudget += removed;
        return map;
    }

}

public struct MapInfo
    {
        public int[,] mapArray;
        public List<(Vector3Int placement, Vector3Int size)> rooms;
        public List<(Vector2Int placement, int type)> enemies;
        public Vector2Int playerStartPos;
        public Vector3Int outlinePlacement;
        public Vector3Int outlineSize;
        public List<(Vector2Int start, Vector2Int end)> componentConnections;
        public int budget;
        public List<(Vector2Int placement, int type)> furnishing;
        public int furnishingBudget;
        public Vector2Int exitPos;
        public int walkableTiles;
        public int WallTiles;
        

        
    }
