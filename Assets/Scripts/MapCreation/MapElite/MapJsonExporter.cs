using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class MapListWrapper
{
    public List<Map> wrappedMaps = new();

    public MapListWrapper(List<Map> maps) {
        this.wrappedMaps = maps;
    }
}

public static class MapJsonExporter
{
    public static void SaveMaps(List<Map> maps, string fileName = "maps.json")
    {
        if (maps == null)
        {
            Debug.LogError("SaveMaps failed: maps list is null.");
            return;
        }

        // Prepare each map before serializing
        foreach (var map in maps)
        {
            if (map != null)
                MapHelpers.PrepareMapForSave(map);
        }

        var wrapper = new MapListWrapper(maps);

        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.dataPath, fileName);

        File.WriteAllText(path, json);

        Debug.Log($"Saved {maps.Count} maps to: {path}");
    }

    public static List<Map> LoadMaps(string fileName = "maps.json")
    {
        string path = Path.Combine(Application.dataPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"LoadMaps: file not found at {path}");
            return new List<Map>();
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("LoadMaps: file was empty.");
            return new List<Map>();
        }

        MapListWrapper wrapper = JsonUtility.FromJson<MapListWrapper>(json);

        if (wrapper == null || wrapper.wrappedMaps == null)
        {
            Debug.LogWarning("LoadMaps: failed to deserialize maps.");
            return new List<Map>();
        }

        foreach (var map in wrapper.wrappedMaps)
        {
            if (map != null)
                MapHelpers.RebuildAfterLoad(map);
        }

        Debug.Log($"Loaded {wrapper.wrappedMaps.Count} maps from: {path}");
        return wrapper.wrappedMaps;
    }
}