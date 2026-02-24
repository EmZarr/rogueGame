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

        // tiles[y][x]
        public List<List<int>> tiles;

        // flattened row-major list: length == width * height
        // IMPORTANT: this exporter flattens in (y-major, x-minor) order using mapArray[x,y]
        public List<int> flatTiles;

        // Fitness
        public float fitness;      // combined
        public float geoFitness;   // slice
        public float enemyFitness; // slice (maps from candidate.enemFitness)
        public float furnFitness;  // slice

        // Behavior slices
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
        var collection = new MapCollection { maps = new List<MapDTO>() };

        foreach (var candidate in candidates)
        {
            if (candidate == null || candidate.mapData == null)
                continue;

            var map = candidate.mapData;

            int width = map.mapArray?.GetLength(0) ?? 0;
            int height = map.mapArray?.GetLength(1) ?? 0;

            // Defensive: avoid null refs in summary metrics
            int roomsCount = map.components?.Count ?? 0;
            int enemiesCount = map.enemies?.Count ?? 0;
            int furnishingCount = map.furnishing?.Count ?? 0;
            int walkableTiles = map.floorTiles?.Count ?? 0;

            int wallTiles = (map.mapArray != null) ? CountWallTiles(map.mapArray) : 0;

            var dto = new MapDTO
            {
                width = width,
                height = height,
                tiles = (map.mapArray != null) ? ConvertMapArrayToNestedList(map.mapArray) : new List<List<int>>(),
                flatTiles = (map.mapArray != null) ? FlattenMapArray(map.mapArray) : new List<int>(),

                // Fitness: combined + slices
                fitness = SafeFloat(candidate.CombinedFitness),
                geoFitness = SafeFloat(candidate.geoFitness),
                enemyFitness = SafeFloat(candidate.enemFitness),
                furnFitness = SafeFloat(candidate.furnFitness),

                // Behaviors
                geoBehavior = ConvertBehaviorToList(candidate.geoBehavior),
                enemyBehavior = ConvertBehaviorToList(candidate.enemyBehavior),
                furnBehavior = ConvertBehaviorToList(candidate.furnBehavior),

                // Summary
                roomsCount = roomsCount,
                enemiesCount = enemiesCount,
                furnishingCount = furnishingCount,
                enemyBudget = map.enemyBudget,
                furnishingBudget = map.furnishingBudget,
                walkableTiles = walkableTiles,
                wallTiles = wallTiles
            };

            collection.maps.Add(dto);
        }

        string json = JsonUtility.ToJson(collection, true);
        string path = Path.Combine(Application.dataPath, filename);
        File.WriteAllText(path, json);

        Debug.Log($"Archive exported to {path} ({collection.maps.Count} maps)");
    }

    // --------------------
    // Helper methods
    // --------------------

    private static float SafeFloat(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return v;
    }

    private static int CountWallTiles(int[,] mapArray)
    {
        // Adjust if you redefine wall IDs.
        // Based on your instantiator: 3,4,5 are walls, plus corner variants 31,32.
        int w = mapArray.GetLength(0);
        int h = mapArray.GetLength(1);
        int count = 0;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int t = mapArray[x, y];
                if (t == 3 || t == 4 || t == 5 || t == 31 || t == 32)
                    count++;
            }
        }

        return count;
    }

    private static List<int> FlattenMapArray(int[,] mapArray)
    {
        int width = mapArray.GetLength(0);
        int height = mapArray.GetLength(1);

        // Row-major by y then x (matches dto.tiles[y][x])
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
        return collection ?? new MapCollection { maps = new List<MapDTO>() };
    }

    public static int[,] MapFromDto(MapDTO dto)
    {
        var arr = new int[dto.width, dto.height];

        // Prefer flatTiles if valid
        if (dto.flatTiles != null && dto.flatTiles.Count == dto.width * dto.height)
        {
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

        // Fallback to nested list
        if (dto.tiles != null && dto.tiles.Count == dto.height)
        {
            for (int y = 0; y < dto.height; y++)
            {
                var row = dto.tiles[y];
                if (row == null) continue;

                int rowCount = Mathf.Min(row.Count, dto.width);
                for (int x = 0; x < rowCount; x++)
                {
                    arr[x, y] = row[x];
                }
            }
        }

        return arr;
    }
}