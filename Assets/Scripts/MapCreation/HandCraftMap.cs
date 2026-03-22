using System;
using System.Collections.Generic;
using UnityEngine;

public class HandCraftMap : MonoBehaviour
{
    [SerializeField] private MapGenerator generator;
    [SerializeField] private List<HandcraftedMapData> handcraftedMaps = new List<HandcraftedMapData>();
    private readonly List<MapCandidate> archive = new List<MapCandidate>();

    private void Start()
    {
        archive.Clear();

        foreach (var data in handcraftedMaps)
        {
            if (data == null)
                continue;

            MapInfo map = CreateEmptyMap();

            BuildRoomGeometry(map, data.rooms);
            PlaceEnemies(map, data.enemies);
            PlaceFurnishing(map, data.furnishing);

            MapCandidate candidate = BuildCandidate(map);
            archive.Add(candidate);
        }

        MapArchiveExporter.ExportArchiveToJson(archive, "handcrafted_maps.json");
    }

    private MapInfo CreateEmptyMap()
    {
        return new MapInfo
        {
            enemies = new List<(Vector2Int, int)>(),
            furnishing = new List<(Vector2Int, int)>(),
            floorTiles = new List<Vector2Int>(),
            enemyBudget = 0,
            furnishingBudget = 0,
            distFromPlayerToEnd = 0,
            components = new List<FloorComponent>()
        };
    }

    private MapCandidate BuildCandidate(MapInfo map)
    {
        MapCandidate candidate = new MapCandidate(map);

        candidate.geoBehavior = new Vector2(
            BehaviorFunctions.GetComponentCountBehavior(map),
            0
        );

        candidate.furnBehavior = new Vector2(
            BehaviorFunctions.FurnishingBehaviorExploration(map),
            BehaviorFunctions.FurnishingBehaviorSafety(map)
        );

        candidate.enemyBehavior = new Vector2(
            BehaviorFunctions.EnemyRoleCompositionBehavior(map.enemies, 20),
            BehaviorFunctions.EnemyDifficultyBehavior(map)
        );

        candidate.geoFitness = FitnessFunctions.GetGeometryFitness(candidate);
        candidate.furnFitness = FitnessFunctions.GetFurnishingFitness(candidate);
        candidate.enemFitness = FitnessFunctions.GetEnemyFitness(candidate);

        return candidate;
    }

    private MapInfo BuildRoomGeometry(MapInfo map, List<Room> rooms)
    {
        if (rooms == null)
            return map;

        foreach (Room room in rooms)
        {
            PlaceRoom(map, room);
        }

        map = generator.buildMapFromComponents(map);
        map = generator.PlaceCorners(map);

        if (map.shortestPath != null)
        {
            map = generator.SetShortestPathTiles(map);
        }

        return map;
    }

    private void PlaceRoom(MapInfo map, Room room)
    {
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
            var newComponent = new FloorComponent();
            newComponent.rooms.Add(room);
            newComponent.UpdateTileList();
            map.components.Add(newComponent);
            return;
        }

        var main = touchingComponents[0];
        main.rooms.Add(room);

        for (int i = 1; i < touchingComponents.Count; i++)
        {
            var other = touchingComponents[i];
            main.rooms.AddRange(other.rooms);
            map.components.Remove(other);
        }

        main.UpdateTileList();
    }

    private void PlaceEnemies(MapInfo map, List<SpawnEntry> enemies)
    {
        if (enemies == null)
            return;

        foreach (var enemy in enemies)
        {
            map.enemies.Add((enemy.position, 40 + enemy.type));

            var component = MapGenerator.GetComponentForTile(map, enemy.position);
            if (component != null)
            {
                float cost = enemy.type switch
                {
                    0 => 2f,
                    1 => 0.5f,
                    _ => 1f
                };

                component.enemiesCount += cost;
            }
        }

        foreach (var (p, t) in map.enemies)
        {
            map.mapArray[p.x, p.y] = t;
        }
    }

    private void PlaceFurnishing(MapInfo map, List<SpawnEntry> furnishing)
    {
        if (furnishing == null)
            return;

        foreach (var item in furnishing)
        {
            map.furnishing.Add((item.position, 11 + item.type));

            var component = MapGenerator.GetComponentForTile(map, item.position);
            if (component == null)
                continue;

            if (item.type == 0 || item.type == 1)
            {
                component.spikeCount++;
            }
            else if (item.type == 2 || item.type == 3)
            {
                component.lootCount++;
            }
        }

        foreach (var (p, t) in map.furnishing)
        {
            map.mapArray[p.x, p.y] = t;
        }
    }
}

[Serializable]
public class HandcraftedMapData
{
    public List<Room> rooms = new List<Room>();
    public List<SpawnEntry> enemies = new List<SpawnEntry>();
    public List<SpawnEntry> furnishing = new List<SpawnEntry>();
}

[Serializable]
public struct SpawnEntry
{
    public Vector2Int position;
    public int type;
}
