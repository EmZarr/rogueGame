using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MapArchiveExporter
{
    [System.Serializable]
    public class MapDTO
    {
        public int width;
        public int height;
        public List<List<int>> tiles; // tiles[y][x]

        // flattened row-major list: length == width * height
        public List<int> flatTiles;

        public float fitness;

        // All three behavior axes
        public List<float> geoBehavior;   // [x, y]
        public List<float> enemyBehavior; // [x, y]
        public List<float> furnBehavior;  // [x, y]

        // Summary metrics
        public int roomsCount;
        public int enemiesCount;
        public int furnishingCount;
        public int enemyBudget;
        public int furnishingBudget;
        public int walkableTiles;
        public int wallTiles;
    }


    [System.Serializable]
    public class MapCollection
    {
        public List<MapDTO> maps;
    }

    public static void ExportArchiveToJson(
    IEnumerable<MapCandidate> candidates,
    string filename = "archive_maps.json")
    {
        var collection = new MapCollection();
        collection.maps = new List<MapDTO>();

        foreach (var candidate in candidates)
        {
            var map = candidate.mapData;

            int width = map.mapArray?.GetLength(0) ?? 0;
            int height = map.mapArray?.GetLength(1) ?? 0;

            MapDTO dto = new MapDTO
            {
                width = width,
                height = height,
                tiles = map.mapArray != null ? ConvertMapArrayToNestedList(map.mapArray) : new List<List<int>>(),
                flatTiles = map.mapArray != null ? FlattenMapArray(map.mapArray) : new List<int>(),
                fitness = candidate.CombinedFitness,

                geoBehavior = ConvertBehaviorToList(candidate.geoBehavior),
                enemyBehavior = ConvertBehaviorToList(candidate.enemyBehavior),
                furnBehavior = ConvertBehaviorToList(candidate.furnBehavior),

                roomsCount = map.components?.Count ?? 0,
                enemiesCount = map.enemies?.Count ?? 0,
                furnishingCount = map.furnishing?.Count ?? 0,
                enemyBudget = map.enemyBudget,
                furnishingBudget = map.furnishingBudget,
                walkableTiles = map.floorTiles.Count,
                // wallTiles = ... if you have it
            };

            collection.maps.Add(dto);
        }

        string json = JsonUtility.ToJson(collection, true);

        string path = Path.Combine(Application.dataPath, filename);
        File.WriteAllText(path, json);
        Debug.Log($"Archive exported to {path} ({collection.maps.Count} maps)");
    }


    // --- helper methods ---

    private static List<int> FlattenMapArray(int[,] mapArray)
    {
        int width = mapArray.GetLength(0);
        int height = mapArray.GetLength(1);

        var flat = new List<int>(width * height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flat.Add(mapArray[x, y]);
            }
        }
        return flat;
    }

    private static List<List<int>> ConvertMapArrayToNestedList(int[,] mapArray)
    {
        int width = mapArray.GetLength(0);
        int height = mapArray.GetLength(1);

        var rows = new List<List<int>>(height);
        for (int y = 0; y < height; y++)
        {
            var row = new List<int>(width);
            for (int x = 0; x < width; x++)
            {
                row.Add(mapArray[x, y]);
            }
            rows.Add(row);
        }
        return rows;
    }

    private static List<float> ConvertBehaviorToList(Vector2 behavior)
    {
        return new List<float> { behavior.x, behavior.y };
    }

    public static MapCollection LoadArchiveFromJson(string filename = "archive_maps.json")
    {
        string path = Path.Combine(Application.dataPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Archive file not found: {path}");
            return new MapCollection { maps = new List<MapDTO>() };
        }

        string json = File.ReadAllText(path);
        var collection = JsonUtility.FromJson<MapCollection>(json);
        return collection;
    }

    public static int[,] MapFromDto(MapDTO dto)
    {
        // create int[,] from dto.tiles or dto.flatTiles
        var arr = new int[dto.width, dto.height];
        int i = 0;
        for (int y = 0; y < dto.height; y++)
        {
            for (int x = 0; x < dto.width; x++)
            {
                arr[x, y] = dto.flatTiles[i++];
            }
        }

        return arr;
    }

}

