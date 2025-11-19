using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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
        map.components = new List <FloorComponent>();
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
        map.endPos = Vector2Int.zero;      
        map.distFromPlayerToEnd = 0f;

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
                    }
                }
            }
        }

        // paint and make corridors
        Vector2Int? previousConnectionTile = null;

        foreach (var component in map.components)
        {
            // pick a random tile in this component
            Vector2Int connectionTile = GetRandomTileInComponent(component);

            // if we have a previous component, connect them now
            if (previousConnectionTile.HasValue)
            {
                DigCorridor(map, previousConnectionTile.Value, connectionTile);
            }
            // now this becomes the previous
            previousConnectionTile = connectionTile;
        }
        Debug.Log("Endpos: " + map.endPos);
        return map;
    }

    MapInfo placeRandomRoom(MapInfo map)
    {
        Vector2Int roomSize = new Vector2Int(Random.Range(4, maxRoomSize.x), Random.Range(4, maxRoomSize.y));
        Vector2Int roomPlacement = new Vector2Int(Random.Range(1, mapSize.x - roomSize.x), Random.Range(1, mapSize.y - roomSize.y));
        Room room = new Room
        {
            placement = roomPlacement,
            size = roomSize
        };

        bool wasInExistingComponent = false;
        foreach (FloorComponent component in map.components)
        {
            if (component.isRoomInComponent(room))
            {
                component.rooms.Add(room);
                wasInExistingComponent = true;
                break;
            }
        }
        if (!wasInExistingComponent)
        {
            FloorComponent newComponent = new FloorComponent();
            newComponent.rooms.Add(room);
            map.components.Add(newComponent);
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

        bool horizontalFirst = Random.value < 0.5f;

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
                if (dist > map.distFromPlayerToEnd)
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


}

public class MapInfo
    {
        public int[,] mapArray;
        public List<FloorComponent> components;
        public List<Vector2Int> floorTiles;
        public List<(Vector2Int placement, int type)> enemies;
        public List<(Vector2Int placement, int type)> furnishing;
        public Vector2Int? playerStartPos;
        public Vector2Int? endPos;
        public List<Vector2Int> shortestPath;
        public float distFromPlayerToEnd;
        public Vector3Int outlinePlacement;
        public Vector3Int outlineSize;
        public List<(Vector2Int start, Vector2Int end)> componentConnections;
        public int enemiesBudget;
        public int furnishingBudget;
    }

public class FloorComponent
{
    // Rooms
    public List<Room> rooms = new List<Room>();
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
            a.XMin <= b.XMax + 1 && a.XMax + 1 >= b.XMin; 
        bool yOverlap =
            a.YMin <= b.YMax + 1 && a.YMax + 1 >= b.YMin;

        return xOverlap && yOverlap;
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


