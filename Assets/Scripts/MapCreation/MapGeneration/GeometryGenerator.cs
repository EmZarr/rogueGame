using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using Unity.VisualScripting;

public static class GeometryGenerator
{
    // Create a fresh set of rooms and chunks for a map.
    public static Map CreateMapGeometry(Map map, int maxChunkSize = 13, int maxChunkAmount = 100)
    {
        map.rooms = new List<Room>();
        // How many chunks are we placing?
        int chunkAmount = UnityEngine.Random.Range(10, maxChunkAmount);

        for (int i = 0; i < chunkAmount; i++)
        {
            AddRandomRoomChunk(map, maxChunkSize);
        }

        return map;
    }

    // Randomly adds and removes chunks from a map
    public static Map MutateMapGeometry(Map map, float mutateSize = 0.2f, int maxChunkSize = 13)
    {
        // We add/remove an amount of chunks equal to 20% of existing. Min 1
       int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.chunkCount() * mutateSize));

        // Randomly either remove or add a chunk. Unless sizes too exterme.
        for (int i = 0; i < amountToMutate; i++) {
            bool addChunk = UnityEngine.Random.value < 0.5f;
            int chunkCounk = map.chunkCount();
            if (chunkCounk < 10) addChunk = true;
            else if (chunkCounk > 100) addChunk = false;

            if (addChunk)
            {
                AddRandomRoomChunk(map, maxChunkSize);
            }
            else {
                RemoveRandomRoomChunk(map);
            }
        }

        return map;

    }

    // Removes a random chunk
    public static void RemoveRandomRoomChunk(Map map)
    {
        if (map.rooms.Count == 0)
            return;

        // Pick random room
        Room room = map.rooms[Random.Range(0, map.rooms.Count)];

        if (room.chunks.Count == 0)
            return;

        // Pick random chunk
        int index = Random.Range(0, room.chunks.Count);
        room.chunks.RemoveAt(index);

        // If room becomes empty, remove it
        if (room.chunks.Count == 0)
        {
            map.rooms.Remove(room);
        }
        else
        {
            map.rooms.Remove(room);
            foreach (RoomChunk chunk in room.chunks)
            {
                AddChunk(map, chunk);
            }
        }
    }

    // Makes a Random rect chunk and places it on the map, while ensuring grouping into rooms
    public static void AddRandomRoomChunk(Map map, int maxChunkSize) {
        // Make a random chunk of random size, shape and position
        Vector2Int chunkSize = new Vector2Int(
            UnityEngine.Random.Range(4, maxChunkSize),
            UnityEngine.Random.Range(4, maxChunkSize)
        );

        Vector2Int chunkPosition = new Vector2Int(
            UnityEngine.Random.Range(0, 100),
            UnityEngine.Random.Range(0, 100)
        );

        RoomChunk ourChunk = new RoomChunk
        {
            position = chunkPosition,
            size = chunkSize
        };

        AddChunk(map, ourChunk);
    }

    private static void AddChunk(Map map, RoomChunk ourChunk)
    {
        // Find all rooms the chunk touches / overlaps
        var roomsWeAreIn = new List<Room>();
        foreach (var room in map.rooms)
        {
            foreach (var chunk in room.chunks)
            {
                if (ourChunk.Overlaps(chunk))
                {
                    roomsWeAreIn.Add(room);
                    break; //don't add same room multiple times
                }
            }
        }

        // If we are not in any rooms, become a new room
        if (roomsWeAreIn.Count == 0)
        {
            var newRoom = new Room(ourChunk.position);
            newRoom.chunks.Add(ourChunk);
            map.rooms.Add(newRoom);
        }
        else
        {
            // Merge all touched rooms into the first room
            Room mainRoom = roomsWeAreIn[0];
            mainRoom.chunks.Add(ourChunk);

            for (int r = 1; r < roomsWeAreIn.Count; r++)
            {
                // add all chunks from the other rooms into the chosen main room
                Room otherRoom = roomsWeAreIn[r];

                foreach (var chunk in otherRoom.chunks)
                {
                    mainRoom.chunks.Add(chunk);
                }

                map.rooms.Remove(otherRoom);
            }
            mainRoom.position = FindBottomLeftTile(mainRoom);
        }
    }

    // Adds all meta data like path, entry and exit, tiles, modifiers. 
    public static void BuildRoomTopology(Map map)
    {
        if (map.rooms == null || map.rooms.Count == 0)
        {
            Debug.Log("WARNING, MAP WITH NO ROOMS");
            return;
        }

        // Make lists of tiles
        BuildRoomTiles(map);

        // We want to find the two closest tiles in two rooms a lot, so we save our results when we do in this dict to avoid repeat search
        var cachedClosestTiles = new Dictionary<(Room, Room), ClosestTilesResult>();

        // 1. Find start + end (furthest pair)
        var (start, end) = FindFurthestRooms(map.rooms, cachedClosestTiles);

        map.startRoom = start;
        map.endRoom = end;

        // 2. Build main path (and mark entry/exit tiles) while making connections
        BuildAndMarkMainPath(map, start, end, cachedClosestTiles);

        // 3. Attach optional rooms while making connections
        AttachOptionalRooms(map, cachedClosestTiles);

        // 4. Calc size factor and order factor
        FindModifiersForRooms(map);

    }

    public static void AddDecorToMap(Map map)
    {
        // 1. make tiles on main path path tiles
        MarkMainPathTiles(map);

        // TODO - Hot stuff
    }

    // Adds all tiles in a room to the room
    public static void BuildRoomTiles(Map map)
    {
        foreach (Room room in map.rooms)
        {
            room.tiles.Clear();
            // Only uniques count
            HashSet<Vector2Int> uniqueTiles = new HashSet<Vector2Int>();

            foreach (RoomChunk chunk in room.chunks)
            {
                for (int x = chunk.XMin; x <= chunk.XMax; x++)
                {
                    for (int y = chunk.YMin; y <= chunk.YMax; y++)
                    {
                        uniqueTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            room.tileSet = uniqueTiles;

            foreach (var tile in uniqueTiles)
            {
                room.tiles.Add(new GridEntry(tile, (int)TerrainType.Standard));
            }
        }
    }

    // Marks tiles on main path as path type
    public static void MarkMainPathTiles(Map map)
    {
        foreach (Room room in map.rooms)
        {
            if (!room.onMainPath)
                continue;
            
            HashSet<Vector2Int> pathTiles = new HashSet<Vector2Int>(
                GetManhattanPath(room.entryTile, room.exitTile)
            );

            for (int i = 0; i < room.tiles.Count; i++)
            {
                var tile = room.tiles[i];

                if (pathTiles.Contains(tile.pos))
                {
                    room.tiles[i] = new GridEntry(tile.pos, (int)TerrainType.Path);
                }
            }
        }
    }

    //Finds modifer for room order and size
    public static void FindModifiersForRooms(Map map)
    {
        int mainPathCount = map.mainPathRooms.Count;

        foreach (Room room in map.rooms)
        {
            //assumes that average room size is 150 (Was 197, and then 128 in test)
            room.sizeModifier = Mathf.Clamp(room.tiles.Count / 150f, 0.5f, 1.5f);

            if (mainPathCount <= 1)
            {
                room.orderModifier = 0f;
            }
            else
            {
                //get the relative order from 0-1
                float t = (room.orderIndex - 1f) / (mainPathCount - 1f); 
                room.orderModifier = Mathf.Clamp(t * 1.5f, 0.5f, 1.5f); 
            }
        }
    }

    //Method that builds the mainpath
    public static Map BuildAndMarkMainPath(Map map, Room start, Room end, Dictionary<(Room, Room), ClosestTilesResult> cached)
    {

        // Clear old
        map.connections.Clear();
        foreach (var room in map.rooms)
        {
            room.onMainPath = false;
            room.orderIndex = 0;
            room.entryTile = new Vector2Int();
            room.exitTile = new Vector2Int();
        }
        //Case if we only have one room
        if (map.rooms.Count == 1)
        {
            Room only = map.rooms[0];
            map.startRoom = only;
            map.endRoom = only;
            map.mainPathRooms = new List<Room> { only };

            only.onMainPath = true;
            only.orderIndex = 1;

            Vector2Int a = only.position;
            Vector2Int b = FindFurthestTileInRoom(only, a);

            only.entryTile = a;
            only.exitTile = b;
            return map;
        }

        var path = new List<Room>();
        var visited = new HashSet<Room>();

        Room current = start;
        visited.Add(current);
        path.Add(current);

        current.onMainPath = true;
        current.orderIndex = 1;

        int order = 1;

        // Starts from start room. For each other room, find distances to that room, and progress to end room.
        // Choose based on combination, biased towards end room. Then repeats with the chosen rooms
        while (current != end)
        {
            Room bestNext = null;
            int bestScore = int.MaxValue;
            int bestStepDist = int.MaxValue;
            Vector2Int bestExit = default;
            Vector2Int bestEntry = default;

            foreach (var candidate in map.rooms)
            {
                if (visited.Contains(candidate))
                    continue;

               
                var stepResult = GetClosestTilesCached(current, candidate, cached);
                var endResult = GetClosestTilesCached(candidate, end, cached);

                int score = endResult.dist + stepResult.dist * 2;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestStepDist = stepResult.dist;
                    bestNext = candidate;
                    bestExit = stepResult.bTile;
                    bestEntry = stepResult.aTile;
                }
            }

            if (bestNext == null)
                break;

            Room previous = current;

            previous.exitTile = bestExit;
            // Add the connections so we can make corridors.
            map.connections.Add(new RoomConnection
            {
                roomA = previous,
                roomB = bestNext,
                tileA = bestExit,
                tileB = bestEntry,
                length = bestStepDist
            });

            current = bestNext;
            visited.Add(current);
            path.Add(current);

            current.entryTile = bestEntry;

            order++;
            current.orderIndex = order;
            current.onMainPath = true;
        }

        current.exitTile = FindFurthestTileInRoom(current, current.entryTile);
        start.entryTile = FindFurthestTileInRoom(start, start.exitTile);
        map.mainPathRooms = path;
        return map;
    }

    // For each optional room, find closest room on main path and make connection to it.
    public static void AttachOptionalRooms(Map map, Dictionary<(Room, Room), ClosestTilesResult> cached)
    {
        var mainSet = new HashSet<Room>(map.mainPathRooms);

        foreach (var room in map.rooms)
        {
            if (mainSet.Contains(room))
                continue;

            Room bestMain = null;
            int bestDist = int.MaxValue;
            Vector2Int bestEntry = default;
            Vector2Int bestMainTile = default;

            foreach (var main in map.mainPathRooms)
            {
                var result = GetClosestTilesCached(room, main, cached);

                if (result.dist < bestDist)
                {
                    bestDist = result.dist;
                    bestMain = main;
                    bestEntry = result.aTile;
                    bestMainTile = result.bTile;
                }
            }

            room.entryTile = bestEntry;
            room.exitTile = FindFurthestTileInRoom(room, bestEntry);
            room.orderIndex = bestMain.orderIndex;
            // Add the connections so we can make corridors.
            map.connections.Add(new RoomConnection
            {
                roomA = room,
                roomB = bestMain,
                tileA = bestEntry,
                tileB = bestMainTile,
                length = bestDist
            });
        }
    }

    // Helpers

    // Find the two tiles in two rooms closest to eachother and their distance
    public static ClosestTilesResult FindClosestTiles(Room a, Room b)
    {
        int bestDist = int.MaxValue;
        Vector2Int bestA = default;
        Vector2Int bestB = default;

        foreach (var tileA in a.tiles)
        {
            Vector2Int ta = tileA.pos;

            foreach (var tileB in b.tiles)
            {
                Vector2Int tb = tileB.pos;
                int d = Mathf.Abs(ta.x - tb.x) + Mathf.Abs(ta.y - tb.y);

                if (d < bestDist)
                {
                    bestDist = d;
                    bestA = ta;
                    bestB = tb;
                }
            }
        }

        return new ClosestTilesResult
        {
            dist = bestDist,
            aTile = bestA,
            bTile = bestB
        };
    }

    // Called version of above. Caches the results to reduce amount of calculations
    public static ClosestTilesResult GetClosestTilesCached(
    Room a,
    Room b,
    Dictionary<(Room, Room), ClosestTilesResult> cache)
    {
        if (cache.TryGetValue((a, b), out var result))
            return result;

        result = FindClosestTiles(a, b);
        cache[(a, b)] = result;

        // store reversed too
        cache[(b, a)] = new ClosestTilesResult
        {
            dist = result.dist,
            aTile = result.bTile,
            bTile = result.aTile
        };

        return result;
    }

    public struct ClosestTilesResult
    {
        public int dist;
        public Vector2Int aTile;
        public Vector2Int bTile;
    }

    // Finds the two rooms furthest away from eachother
    public static (Room a, Room b) FindFurthestRooms(List<Room> rooms, Dictionary<(Room, Room), ClosestTilesResult> cached)
    {
        int bestDist = -1;
        Room bestA = null;
        Room bestB = null;

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var result = GetClosestTilesCached(rooms[i], rooms[j], cached);

                if (result.dist > bestDist)
                {
                    bestDist = result.dist;
                    bestA = rooms[i];
                    bestB = rooms[j];
                }
            }
        }

        return (bestA, bestB);
    }



    // Find the tile in a room furthest away from another tile
    public static Vector2Int FindFurthestTileInRoom(Room room, Vector2Int fromTile)
    {
        int bestDist = -1;
        Vector2Int bestTile = fromTile;

        foreach (var tileData in room.tiles)
        {
            Vector2Int tile = tileData.pos;
            int dist = Mathf.Abs(tile.x - fromTile.x) + Mathf.Abs(tile.y - fromTile.y);

            if (dist > bestDist)
            {
                bestDist = dist;
                bestTile = tile;
            }
        }

        return bestTile;
    }

    // Finds the bottom left tile in a room
    public static Vector2Int FindBottomLeftTile(Room room)
    {
        RoomChunk best = room.chunks[0];

        foreach (var chunk in room.chunks)
        {
            if (chunk.XMin < best.XMin || (chunk.XMin == best.XMin && chunk.YMin < best.YMin))
            {
                best = chunk;
            }
        }

        return new Vector2Int(best.XMin, best.YMin);
    }

    // Gets path from one tile to another in a room
    public static List<Vector2Int> GetManhattanPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = start;
        path.Add(current);

        while (current.x != end.x)
        {
            current.x += current.x < end.x ? 1 : -1;
            path.Add(current);
        }

        while (current.y != end.y)
        {
            current.y += current.y < end.y ? 1 : -1;
            path.Add(current);
        }

        return path;
    }
}
