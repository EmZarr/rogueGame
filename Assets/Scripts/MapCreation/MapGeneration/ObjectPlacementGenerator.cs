using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
using static UnityEditor.Recorder.OutputPath;

public static class ObjectPlacementGenerator
{
    public static Map CreateEnemiesOnMap(Map map)
    {
        return CreateEnemiesOnMap(map, 10, new Vector2(0.25f, 6f));
    }

    public static Map CreateEnemiesOnMap(Map map, int baseBudget)
    {
        return CreateEnemiesOnMap(map, baseBudget, new Vector2(0.25f, 6f));
    }

    public static Map CreateEnemiesOnMap(Map map, int baseBudget, Vector2 budgetModifierRange)
    {
        HashSet<Vector2Int> occupiedPositions = new();
        foreach (Room room in map.rooms)
        {
            foreach (var e in room.enemies)
                occupiedPositions.Add(e.pos);

            foreach (var l in room.loot)
                occupiedPositions.Add(l.pos);

            foreach (var o in room.obstacles)
                occupiedPositions.Add(o.pos);

            room.enemyBudget = baseBudget
            * room.sizeModifier
            * room.orderModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);
            room.enemyBudgetUsed = 0f;

            while (room.enemyBudgetUsed < room.enemyBudget) {
                PlaceRandomEnemy(room, occupiedPositions);
            }

        }
        return map;
    }

    public static void PlaceRandomEnemy(Room room, HashSet<Vector2Int> occupied)
    {
        if (room.enemies == null || room.enemies.Count == 0)
            return;

        // Find Random unoccupied tile
        Vector2Int tile;
        int tries = 0;
        do
        {
            tile = room.tiles[Random.Range(0, room.tiles.Count)].pos;
            tries++;
        }
        while (occupied.Contains(tile) && tries < 1000);

        if (tries >= 1000) {
            Debug.Log("WARNING, FULLY OCCUPIED ROOM");
            return;
        }

        // Choose enemy type randomly, add enemy, update budget and occupied
        EnemyType randomType = MapHelpers.EnemyTypes[Random.Range(0, MapHelpers.EnemyTypes.Length)];
        room.enemies.Add((tile, (int)randomType));
        room.enemyBudgetUsed += MapHelpers.EnemyCosts[randomType];
        occupied.Add(tile);
    }

    public static void RemoveRandomEnemy(Room room, HashSet<Vector2Int> occupied)
    {
        if (room.enemies == null || room.enemies.Count == 0)
            return;

        int index = Random.Range(0, room.enemies.Count);

        var enemy = room.enemies[index];

        // Remove from list
        room.enemies.RemoveAt(index);

        // Update budget
        EnemyType type = (EnemyType)enemy.type;
        room.enemyBudgetUsed -= MapHelpers.EnemyCosts[type];

        // Free tile
        occupied.Remove(enemy.pos);
    }

    public static Map CreateLootOnMap(Map map, int baseBudget = 10)
    {

    }

    public static Map CreateObstaclesOnMap(Map map, int baseBudget = 10)
    {

    }

    public static Map MutateEnemies(Map map, float mutateSize = 0.2f)
    {
        HashSet<Vector2Int> occupiedPositions = new();
        foreach (Room room in map.rooms)
        {
            foreach (var e in room.enemies)
                occupiedPositions.Add(e.pos);

            foreach (var l in room.loot)
                occupiedPositions.Add(l.pos);

            foreach (var o in room.obstacles)
                occupiedPositions.Add(o.pos);
        }
        int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.enemyCount() * mutateSize));

        for (int i = 0; i < amountToMutate; i++)
        {
            Room roomToMutate =  map.rooms[Random.Range(0, map.rooms.Count)];
            RemoveRandomEnemy(roomToMutate, occupiedPositions);
            PlaceRandomEnemy(roomToMutate, occupiedPositions);
        }
        return map;
    }

    public static Map MutateLoot(Map map, float mutateSize = 0.2f)
    {

    }

    public static Map MutateObstacles(Map map, float mutateSize = 0.2f)
    {

    }

}
