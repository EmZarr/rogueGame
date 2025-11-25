using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    [Header("Geometry")]
    [SerializeField] Vector3Int mapSize;
    [SerializeField] Vector3Int maxRoomSize;
    [SerializeField] int maxRoomAmount;

    [Header("Enemies")]
    [SerializeField] int amountOfEnemyTypes;
    [SerializeField] int startingBudget;


    [Header("Furnishing")]
    [SerializeField] int maxAmountFurnishing;
    [SerializeField] int amountOfFurnishingTypes;


    [Header("Controls")]
    public InputActionReference placeObjects;
    public InputActionReference remakeMap;
    public InputActionReference mutateMap;
    public InputActionReference mutatePlacements;
    public InputActionReference mutateContent;

    public MapInfo currentMap;

    MapInstantiator mapInstantiator;

    void Awake()
    {
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
    }
    void Start()
    {
        //Debug.Log("Helo?");
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
        placeObjects.action.performed += ctx => { currentMap = PlaceObjects(currentMap); };
        remakeMap.action.performed += ctx => RemakeMap();
        mutateMap.action.performed += ctx => { currentMap = MutateMap(currentMap); };
        mutatePlacements.action.performed += ctx => { currentMap = MutatePlacements(currentMap); };
        mutateContent.action.performed += ctx => { currentMap = MutateContent(currentMap); };
    }

    private MapInfo PlaceObjects(MapInfo map)
    {
        map = placeFurnishing(map);
        map = placeEnemies(map);
        mapInstantiator.makeMap(map.mapArray);
        return map;
    }

    public void RemakeMap()
    {
        currentMap = new MapInfo
        {
            enemies = new List<(Vector2Int, int)>(),
            furnishing = new List<(Vector2Int, int)>(),
            floorTiles = new List<Vector2Int>(),
            enemiesBudget = startingBudget,
            furnishingBudget = maxAmountFurnishing,
            distFromPlayerToEnd = 0,
        };
        currentMap = makeRoomGeometry(currentMap);
        //currentMap = PlaceObjects(currentMap);
        mapInstantiator.makeMap(currentMap.mapArray);
    }

    public MapInfo MutateMap(MapInfo map)
    {
        map = mutateGeometry(map);
        map = PlaceObjects(map);
        //mapInstantiator.makeMap(map.mapArray);
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
        map.components = new List<FloorComponent>();
        int roomAmount = Random.Range(1, maxRoomAmount);
        for (int i = 0; i < roomAmount; i++)
        {
            map = placeRandomRoom(map);
        }

        return buildMapFromComponents(map);
    }

    MapInfo buildMapFromComponents(MapInfo map)
    {
        // clear map
        map.mapArray = new int[mapSize.x, mapSize.y];
        map.floorTiles = new List<Vector2Int>();
        map.playerStartPos = null;
        map.endPos = null;
        map.distFromPlayerToEnd = 0f;
        map.mapSize = 0;

        // paint 
        foreach (var component in map.components)
        {
            // paint this component's rooms
            foreach (var room in component.rooms)
            {
                for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
                {
                    for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                    {
                        SetFloorAndAutoWalls(map, x, y, false);
                        map.mapSize++;
                    }
                }
            }
        }

        // paint and make corridors
        ConnectComponentsByNearest(map);
        // compute shortest path once the map geometry is ready
        if (map.playerStartPos.HasValue && map.endPos.HasValue)
        {
            map.shortestPath = FindPathAStar(map, map.playerStartPos.Value, map.endPos.Value);

            if (map.shortestPath == null)
            {
                Debug.LogWarning("No path found between player start and end!");
            }
        }


        /*foreach (var component in map.components)
        {
            // paint this component's rooms
            foreach (var room in component.rooms)
            {
                for (int x = room.placement.x; x < room.placement.x + room.size.x; x++)
                {
                    for (int y = room.placement.y; y < room.placement.y + room.size.y; y++)
                    {
                        if (component.onMainPath)
                        {
                            if (map.mapArray[x, y] == 1) map.mapArray[x,y] = 98;
                        }
                    }
                }
            }
        }*/
        
        /*foreach (var component in map.components) {
            if (component.onMainPath)
            {
                if (component.onMainPath)
                {
                    if (map.mapArray[component.entryTile.Value.x, component.entryTile.Value.y] == 1) map.mapArray[component.entryTile.Value.x, component.entryTile.Value.y] = 98;
                    if (map.mapArray[component.entryTile.Value.x, component.entryTile.Value.y] == 1) map.mapArray[component.exitTile.Value.x, component.exitTile.Value.y] = 98;
                }
            }
        }*/
        return map;
    }

    MapInfo placeRandomRoom(MapInfo map)
    {
        Vector2Int roomSize = new Vector2Int(
            Random.Range(4, maxRoomSize.x),
            Random.Range(4, maxRoomSize.y)
        );

        Vector2Int roomPlacement = new Vector2Int(
            Random.Range(1, mapSize.x - roomSize.x),
            Random.Range(1, mapSize.y - roomSize.y)
        );

        Room room = new Room
        {
            placement = roomPlacement,
            size = roomSize
        };

        // Find ALL components this room touches
        var touchingComponents = new List<FloorComponent>();
        foreach (var component in map.components)
        {
            if (component.isRoomInComponent(room))
            {
                touchingComponents.Add(component);
            }
        }

        if (touchingComponents.Count == 0)
        {
            // New isolated component
            var newComponent = new FloorComponent();
            newComponent.rooms.Add(room);
            map.components.Add(newComponent);
        }
        else
        {
            // Merge all touching components into the first one
            var main = touchingComponents[0];
            main.rooms.Add(room);

            for (int i = 1; i < touchingComponents.Count; i++)
            {
                var other = touchingComponents[i];
                main.rooms.AddRange(other.rooms);
                map.components.Remove(other);
            }
        }

        return map;
    }


    MapInfo placeEnemies(MapInfo map)
    {
        // collect candidate floor tiles
        List<Vector2Int> candidates = new List<Vector2Int>(map.floorTiles);

        // shuffle candidates
        for (int i = 0; i < candidates.Count; i++)
        {
            int swapIndex = Random.Range(i, candidates.Count);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        var occupied = new HashSet<Vector2Int>(
        map.enemies.Select(e => e.placement)
        .Concat(map.furnishing.Select(f => f.placement)));

        // place enemies not too close to each other
        foreach (var pos in candidates)
        {
            if (map.enemiesBudget <= 0)
                break;
            // check distance to enemies and furnishing
            bool tooClose = false;
            foreach (var p in occupied)
            {
                if (p == pos)
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
            map.enemiesBudget--;
        }

        foreach (var (p, t) in map.enemies)
        {
            map.mapArray[p.x, p.y] = 6 + t;
        }

        return map;
    }

    MapInfo placeFurnishing(MapInfo map)
    {
        // collect candidate floor tiles
        List<Vector2Int> candidates = new List<Vector2Int>(map.floorTiles);

        // shuffle candidates
        for (int i = 0; i < candidates.Count; i++)
        {
            int swapIndex = Random.Range(i, candidates.Count);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        var occupied = new HashSet<Vector2Int>(
        map.enemies.Select(e => e.placement)
        .Concat(map.furnishing.Select(f => f.placement)));

        foreach (var pos in candidates)
        {
            if (map.furnishingBudget <= 0)
                break;
            // check distance to enemies and furnishing
            bool tooClose = false;
            foreach (var p in occupied)
            {
                if (p == pos)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;

            // place funrnishing
            int furnishType = Random.Range(0, amountOfFurnishingTypes);
            map.furnishing.Add((pos, furnishType));
            map.furnishingBudget--;
        }

        foreach (var (p, t) in map.furnishing)
        {
            map.mapArray[p.x, p.y] = 3 + t;
        }

        return map;
    }

    // Remove enemies that are no longer on floor tiles
    MapInfo ValidateEnemiesAgainstMap(MapInfo map)
    {
        int oldCount = map.enemies.Count;
        var valid = new List<(Vector2Int placement, int type)>();
        var floorSet = new HashSet<Vector2Int>(map.floorTiles);
        foreach (var (pos, type) in map.enemies)
        {
            if (floorSet.Contains(pos)) // still floor
            {
                valid.Add((pos, type));
            }
        }

        map.enemies = valid;

        int removed = oldCount - map.enemies.Count;
        map.enemiesBudget += removed;
        return map;
    }

    MapInfo ValidateFurnishingAgainstMap(MapInfo map)
    {
        int oldCount = map.furnishing.Count;
        var valid = new List<(Vector2Int placement, int type)>();
        var floorSet = new HashSet<Vector2Int>(map.floorTiles);
        foreach (var (pos, type) in map.furnishing)
        {
            if (floorSet.Contains(pos)) // still floor
            {
                valid.Add((pos, type));
            }
        }

        map.furnishing = valid;

        int removed = oldCount - map.furnishing.Count;
        map.furnishingBudget += removed;
        return map;
    }


    MapInfo mutateGeometry(MapInfo map)
    {
        // if no rooms, must add
        int totalRooms = 0;
        foreach (var c in map.components)
            totalRooms += c.rooms.Count;
        // randomly decide to add or remove a room
        bool addRoom = totalRooms == 0 || Random.value < 0.5f;

        if (addRoom)
        {
            map = placeRandomRoom(map);
        }
        else
        {
            // remove a random room from a random component
            if (map.components.Count > 0)
            {
                int compIdx = Random.Range(0, map.components.Count);
                FloorComponent comp = map.components[compIdx];
                if (comp.rooms.Count > 0)
                {
                    int roomIdx = Random.Range(0, comp.rooms.Count);
                    comp.rooms.RemoveAt(roomIdx);
                    // if component has no more rooms, remove it
                    if (comp.rooms.Count == 0)
                    {
                        map.components.RemoveAt(compIdx);
                    }
                }
            }
        }
        // rebuild map
        map = buildMapFromComponents(map);
        // validate existing enemies and furnishing
        map = ValidateEnemiesAgainstMap(map);
        map = ValidateFurnishingAgainstMap(map);
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
        map.enemiesBudget++;

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

    Vector2Int GetRandomTileInComponent(FloorComponent comp)
    {
        var room = comp.rooms[Random.Range(0, comp.rooms.Count)];
        int x = Random.Range(room.XMin, room.XMax + 1);
        int y = Random.Range(room.YMin, room.YMax + 1);
        return new Vector2Int(x, y);
    }

    // Makes floor tiles in a path between two tiles
    void DigCorridor(MapInfo map, Vector2Int from, Vector2Int to)
    {
        int x = from.x;
        int y = from.y;

        bool horizontalFirst = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.y - from.y);

        if (horizontalFirst)
        {
            while (x != to.x)
            {
                SetFloorAndAutoWalls(map, x, y, true);
                x += (to.x > x) ? 1 : -1;
            }
            while (y != to.y)
            {
                SetFloorAndAutoWalls(map, x, y, true);
                y += (to.y > y) ? 1 : -1;
            }
        }
        else
        {
            while (y != to.y)
            {
                SetFloorAndAutoWalls(map, x, y, true);
                y += (to.y > y) ? 1 : -1;
            }
            while (x != to.x)
            {
                SetFloorAndAutoWalls(map, x, y, true);
                x += (to.x > x) ? 1 : -1;
            }
        }

        SetFloorAndAutoWalls(map, x, y, true);
    }


    void SetFloorAndAutoWalls(MapInfo map, int x, int y, bool corridor)
    {
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
            return;

        // Set floor and if not floor before, add to floor list
        if (map.mapArray[x, y] != 1 && map.mapArray[x, y] != 100 && map.mapArray[x, y] != 99)
        {
            // if player not set yet, place player.
            if (!map.playerStartPos.HasValue)
            {
                map.playerStartPos = new Vector2Int(x, y);
                map.mapArray[x, y] = 100;
            }
            else
            {
                var pos = new Vector2Int(x, y);
                float dist = Vector2Int.Distance(pos, map.playerStartPos.Value);
                if (!map.endPos.HasValue || dist > map.distFromPlayerToEnd)
                {
                    if (map.endPos.HasValue)
                    {
                        var oldExit = map.endPos.Value;
                        map.mapArray[oldExit.x, oldExit.y] = 1;
                        if (!corridor) map.floorTiles.Add(oldExit);
                    }
                    map.endPos = pos;
                    map.mapArray[x, y] = 99;
                    map.distFromPlayerToEnd = dist;
                }
                else
                {
                    map.mapArray[x, y] = 1;
                    if (!corridor) map.floorTiles.Add(pos);
                }
            }
        }

        // 4-neighbor walls
        TrySetWall(map, x + 1, y);
        TrySetWall(map, x - 1, y);
        TrySetWall(map, x, y + 1);
        TrySetWall(map, x, y - 1);
    }

    void TrySetWall(MapInfo map, int x, int y)
    {
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
            return;

        if (map.mapArray[x, y] == 0)   // only turn empty into wall
            map.mapArray[x, y] = 2;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
    {
        // 4-directional grid movement (no diagonals)
        yield return new Vector2Int(p.x + 1, p.y);
        yield return new Vector2Int(p.x - 1, p.y);
        yield return new Vector2Int(p.x, p.y + 1);
        yield return new Vector2Int(p.x, p.y - 1);
    }

    List<Vector2Int> FindPathAStar(MapInfo map, Vector2Int start, Vector2Int goal)
    {
        // Open set: nodes to be evaluated
        var openSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        // Cost from start to a node
        var gScore = new Dictionary<Vector2Int, int>();
        // Estimated total cost (g + heuristic)
        var fScore = new Dictionary<Vector2Int, int>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            // Node in openSet with lowest fScore
            Vector2Int current = GetLowestFScore(openSet, fScore);

            // Reached goal
            if (current == goal)
            {
                return ReconstructPathAndMarkComponents(map, cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (map.mapArray[neighbor.x, neighbor.y] == 2)
                    continue;

                int tentativeG = gScore[current] + 1; // cost between neighbors is 1

                if (!gScore.TryGetValue(neighbor, out int existingG) || tentativeG < existingG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // No path
        return null;
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance works well on 4-directional grids
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    Vector2Int GetLowestFScore(HashSet<Vector2Int> openSet, Dictionary<Vector2Int, int> fScore)
    {
        Vector2Int best = default;
        int bestScore = int.MaxValue;
        bool first = true;

        foreach (var node in openSet)
        {
            if (!fScore.TryGetValue(node, out int score))
                score = int.MaxValue;

            if (first || score < bestScore)
            {
                best = node;
                bestScore = score;
                first = false;
            }
        }

        return best;
    }
    List<Vector2Int> ReconstructPathAndMarkComponents(
    MapInfo map,
    Dictionary<Vector2Int, Vector2Int> cameFrom,
    Vector2Int current)
    {
        // Build the path from goal back to start
        var pathReversed = new List<Vector2Int> { current };

        while (cameFrom.TryGetValue(current, out Vector2Int prev))
        {
            current = prev;
            pathReversed.Add(current);
        }

        pathReversed.Reverse(); // now it�s start -> goal
        var path = pathReversed;

        // Reset component flags
        foreach (var comp in map.components)
        {
            comp.onMainPath = false;
            comp.entryTile = null;
            comp.exitTile = null;
        }

        // Walk the path once and mark components
        FloorComponent currentComp = null;
        Vector2Int previousTile = default;
        bool hasPreviousTile = false;

        foreach (var tile in path)
        {
            FloorComponent tileComp = GetComponentForTile(map, tile);

            if (tileComp != currentComp)
            {
                // leaving previous component
                if (currentComp != null && hasPreviousTile)
                {
                    currentComp.onMainPath = true;
                    currentComp.exitTile = previousTile;
                }

                // entering new component
                currentComp = tileComp;
                if (currentComp != null && !currentComp.entryTile.HasValue)
                {
                    currentComp.onMainPath = true;
                    currentComp.entryTile = tile;
                }
            }

            previousTile = tile;
            hasPreviousTile = true;
        }

        // close last component
        if (currentComp != null && hasPreviousTile)
        {
            currentComp.onMainPath = true;
            currentComp.exitTile = previousTile;
        }

        return path;
    }


    FloorComponent GetComponentForTile(MapInfo map, Vector2Int tile)
    {
        foreach (var comp in map.components)
        {
            if (comp.ContainsTile(tile))
                return comp;
        }
        return null; // corridors / outside rooms
    }

    (float dist, Vector2Int from, Vector2Int to) FindClosestTiles(FloorComponent a, FloorComponent b)
    {
        float bestDist = float.MaxValue;
        Vector2Int bestA = default;
        Vector2Int bestB = default;

        foreach (var ra in a.rooms)
        {
            for (int ax = ra.XMin; ax <= ra.XMax; ax++)
            {
                for (int ay = ra.YMin; ay <= ra.YMax; ay++)
                {
                    var tileA = new Vector2Int(ax, ay);

                    foreach (var rb in b.rooms)
                    {
                        for (int bx = rb.XMin; bx <= rb.XMax; bx++)
                        {
                            for (int by = rb.YMin; by <= rb.YMax; by++)
                            {
                                var tileB = new Vector2Int(bx, by);

                                float d = Mathf.Abs(tileA.x - tileB.x) + Mathf.Abs(tileA.y - tileB.y); // manhattan

                                if (d < bestDist)
                                {
                                    bestDist = d;
                                    bestA = tileA;
                                    bestB = tileB;
                                }
                            }
                        }
                    }
                }
            }
        }

        return (bestDist, bestA, bestB);
    }

    void ConnectComponentsByNearest(MapInfo map)
    {
        var comps = map.components;
        if (comps == null || comps.Count < 2)
            return;

        // Prim-style MST: grow a connected set by adding the closest remaining component.
        var connected = new HashSet<FloorComponent>();
        var remaining = new HashSet<FloorComponent>(comps);

        // Start from an arbitrary component
        var start = comps[0];
        connected.Add(start);
        remaining.Remove(start);

        while (remaining.Count > 0)
        {
            float bestDist = float.MaxValue;
            FloorComponent bestFromComp = null;
            FloorComponent bestToComp = null;
            Vector2Int bestFromTile = default;
            Vector2Int bestToTile = default;

            // Look at every edge between connected and remaining sets, pick the shortest
            foreach (var fromComp in connected)
            {
                foreach (var toComp in remaining)
                {
                    var (dist, fromTile, toTile) = FindClosestTiles(fromComp, toComp);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFromComp = fromComp;
                        bestToComp = toComp;
                        bestFromTile = fromTile;
                        bestToTile = toTile;
                    }
                }
            }

            // Dig the corridor along that best edge
            DigCorridor(map, bestFromTile, bestToTile);

            // Mark this component as now connected
            connected.Add(bestToComp);
            remaining.Remove(bestToComp);
        }
    }

}

public class MapInfo
{
    public int[,] mapArray;
    public int mapSize;
    public List<FloorComponent> components;
    public List<Vector2Int> floorTiles;
    public List<(Vector2Int placement, int type)> enemies;
    public List<(Vector2Int placement, int type)> furnishing;
    public Vector2Int? playerStartPos;
    public Vector2Int? endPos;
    public float distFromPlayerToEnd;
    public List<Vector2Int> shortestPath;
    public int enemiesBudget;
    public int furnishingBudget;
}

public class FloorComponent
{
    // Rooms
    public List<Room> rooms = new List<Room>();

    public bool onMainPath = false;
    public Vector2Int? entryTile;
    public Vector2Int? exitTile;
    public bool isRoomInComponent(Room room)
    {
        foreach (var r in rooms)
        {
            if (RoomsTouchOrOverlap(r, room))
                return true;
        }
        return false;
    }

    static bool RoomsTouchOrOverlap(Room a, Room b)
    {
        // inclusive bounds
        bool xOverlap =
            a.XMin <= b.XMax && a.XMax >= b.XMin;
        bool yOverlap =
            a.YMin <= b.YMax && a.YMax >= b.YMin;

        return xOverlap && yOverlap;
    }

    public bool ContainsTile(Vector2Int p)
    {
        foreach (var r in rooms)
        {
            if (p.x >= r.XMin && p.x <= r.XMax &&
                p.y >= r.YMin && p.y <= r.YMax)
                return true;
        }
        return false;
    }
}

public struct Room
{
    public Vector2Int placement;
    public Vector2Int size;

    public int XMin => placement.x;
    public int YMin => placement.y;
    public int XMax => placement.x + size.x - 1;
    public int YMax => placement.y + size.y - 1;
}

