using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.Timeline;
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
        // Makes a occupied tile list
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);
        foreach (Room room in map.rooms)
        {
            // Finds budget based on size, order base and randomness
            room.enemyBudget = baseBudget
            * room.sizeModifier
            * room.orderModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);
            room.enemyBudgetUsed = 0f;
            // Place enemies
            while (room.enemyBudgetUsed < room.enemyBudget) {
                bool success = PlaceRandomEnemy(room, occupiedPositions);
                if (!success) break;
            }

        }
        return map;
    }

    public static Map MutateEnemies(Map map)
    {
        return MutateEnemies(map, 0.2f, new Vector2(0.25f, 6f), 10);
    }

    public static Map MutateEnemies(Map map, float mutateSize, Vector2 budgetModifierRange, int baseBudget)
    {

        // add occupied tiles
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);

        // Mutates a few budgets, and adds/removes enemies accordingly
        int budgetsToMutate = Mathf.Max(1, Mathf.RoundToInt(map.rooms.Count * mutateSize));
        for (int i = 0; i < budgetsToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];
            roomToMutate.enemyBudget = baseBudget
            * roomToMutate.sizeModifier
            * roomToMutate.orderModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);

            while (roomToMutate.enemyBudgetUsed > roomToMutate.enemyBudget)
            {
                RemoveRandomEnemy(roomToMutate, occupiedPositions);
            }
            while (roomToMutate.enemyBudgetUsed < roomToMutate.enemyBudget)
            {
                bool success = PlaceRandomEnemy(roomToMutate, occupiedPositions);
                if (!success) break;
            }

        }

        // Randomly removes and readds an amount of enemies
        int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.enemyCount() * mutateSize));

        for (int i = 0; i < amountToMutate; i++)
        {
            Room roomToMutate =  map.rooms[Random.Range(0, map.rooms.Count)];

            RemoveRandomEnemy(roomToMutate, occupiedPositions);
            bool success = PlaceRandomEnemy(roomToMutate, occupiedPositions);
            if (!success) break;
        }
        return map;
    }

    public static bool PlaceRandomEnemy(Room room, HashSet<Vector2Int> occupied)
    {

        // Find Random unoccupied tile
        if (!TryGetRandomFreeTile(room, occupied, out Vector2Int tile)) return false;

        // Choose enemy type randomly, add enemy, update budget and occupied
        EnemyType randomType = MapHelpers.EnemyTypes[Random.Range(0, MapHelpers.EnemyTypes.Length)];
        room.enemies.Add((tile, (int)randomType));
        room.enemyBudgetUsed += MapHelpers.EnemyCosts[randomType];
        occupied.Add(tile);
        return true;
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

    public static Map CreateLootOnMap(Map map)
    {
        return CreateLootOnMap(map, 4, new Vector2(0.5f, 3f));
    }
    public static Map CreateLootOnMap(Map map, int baseBudget, Vector2 budgetModifierRange)
    {
        // Makes a occupied tile list
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);
        foreach (Room room in map.rooms)
        {
            // Finds budget based on size, and randomness
            room.lootBudget = baseBudget
            * room.sizeModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);
            room.lootBudgetUsed = 0f;
            // Place enemies
            while (room.lootBudgetUsed < room.lootBudget)
            {
                bool success = PlaceRandomLoot(room, occupiedPositions);
                if (!success) break;
            }

        }
        return map;
    }

    public static Map MutateLoot(Map map)
    {
        return MutateLoot(map, 0.2f, new Vector2(0.5f, 3f), 4);
    }
    public static Map MutateLoot(Map map, float mutateSize, Vector2 budgetModifierRange, int baseBudget)
    {
        // add occupied tiles
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);

        // Mutates a few budgets, and adds/removes enemies accordingly
        int budgetsToMutate = Mathf.Max(1, Mathf.RoundToInt(map.rooms.Count * mutateSize));
        for (int i = 0; i < budgetsToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];
            roomToMutate.lootBudget = baseBudget
            * roomToMutate.sizeModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);

            while (roomToMutate.lootBudgetUsed > roomToMutate.lootBudget)
            {
                RemoveRandomLoot(roomToMutate, occupiedPositions);
            }
            while (roomToMutate.lootBudgetUsed < roomToMutate.lootBudget)
            {
                bool success = PlaceRandomLoot(roomToMutate, occupiedPositions);
                if (!success) break;
            }

        }

        // Randomly removes and readds an amount of loot
        int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.lootCount() * mutateSize));

        for (int i = 0; i < amountToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];

            RemoveRandomLoot(roomToMutate, occupiedPositions);
            bool success = PlaceRandomLoot(roomToMutate, occupiedPositions);
            if (!success) break;
        }
        return map;
    }

    public static bool PlaceRandomLoot(Room room, HashSet<Vector2Int> occupied)
    {
        // Find Random unoccupied tile
        if (!TryGetRandomFreeTile(room, occupied, out Vector2Int tile)) return false;

        // Choose loot type randomly, add loot, update budget and occupied
        LootType randomType = MapHelpers.LootTypes[Random.Range(0, MapHelpers.LootTypes.Length)];
        room.loot.Add((tile, (int)randomType));
        room.lootBudgetUsed += 1;
        occupied.Add(tile);
        return true;
    }

    public static void RemoveRandomLoot(Room room, HashSet<Vector2Int> occupied)
    {
        if (room.loot == null || room.loot.Count == 0)
            return;

        int index = Random.Range(0, room.loot.Count);

        var loot = room.loot[index];

        // Remove from list
        room.loot.RemoveAt(index);

        // Update budget
        room.lootBudgetUsed -= 1;

        // Free tile
        occupied.Remove(loot.pos);
    }
    public static Map CreateObstaclesOnMap(Map map)
    {
        return CreateObstaclesOnMap(map, 4, new Vector2(0.5f, 3f));
    }
    public static Map CreateObstaclesOnMap(Map map, int baseBudget, Vector2 budgetModifierRange)
    {
        // Makes a occupied tile list
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);
        foreach (Room room in map.rooms)
        {
            // Finds budget based on size, and randomness
            room.obstacleBudget = baseBudget
            * room.sizeModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);
            room.obstacleBudgetUsed = 0f;
            // Place enemies
            while (room.obstacleBudgetUsed < room.obstacleBudget)
            {
                bool success = PlaceRandomObstacle(room, occupiedPositions);
                if (!success) break;
            }

        }
        return map;
    }
    public static Map MutateObstacles(Map map)
    {
        return MutateObstacles(map, 0.2f, new Vector2(0.5f, 3f), 4);
    }
    public static Map MutateObstacles(Map map, float mutateSize, Vector2 budgetModifierRange, int baseBudget)
    {
        // add occupied tiles
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);

        // Mutates a few budgets, and adds/removes obstacles accordingly
        int budgetsToMutate = Mathf.Max(1, Mathf.RoundToInt(map.rooms.Count * mutateSize));
        for (int i = 0; i < budgetsToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];
            roomToMutate.obstacleBudget = baseBudget
            * roomToMutate.sizeModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y);

            while (roomToMutate.obstacleBudgetUsed > roomToMutate.obstacleBudget)
            {
                RemoveRandomObstacle(roomToMutate, occupiedPositions);
            }
            while (roomToMutate.obstacleBudgetUsed < roomToMutate.obstacleBudget)
            {
                bool success = PlaceRandomObstacle(roomToMutate, occupiedPositions);
                if (!success) break;
            }

        }

        // Randomly removes and readds an amount of loot
        int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.obstacleCount() * mutateSize));

        for (int i = 0; i < amountToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];

            RemoveRandomObstacle(roomToMutate, occupiedPositions);
            bool success = PlaceRandomObstacle(roomToMutate, occupiedPositions);
            if (!success) break;
        }
        return map;
    }

    public static bool PlaceRandomObstacle(Room room, HashSet<Vector2Int> occupied)
    {
        // Find Random unoccupied tile
        if (!TryGetRandomFreeTile(room, occupied, out Vector2Int tile)) return false;

        // Choose loot type randomly, add loot, update budget and occupied
        ObstacleType randomType = MapHelpers.ObstacleTypes[Random.Range(0, MapHelpers.ObstacleTypes.Length)];
        room.obstacles.Add((tile, (int)randomType));
        room.obstacleBudgetUsed += 1;
        occupied.Add(tile);
        return true;
    }

    public static void RemoveRandomObstacle(Room room, HashSet<Vector2Int> occupied)
    {
        if (room.obstacles == null || room.obstacles.Count == 0)
            return;

        int index = Random.Range(0, room.obstacles.Count);

        var obstacle = room.obstacles[index];

        // Remove from list
        room.obstacles.RemoveAt(index);

        // Update budget 
        room.obstacleBudgetUsed -= 1;

        // Free tile
        occupied.Remove(obstacle.pos);
    }


    // Helpers

    private static HashSet<Vector2Int> GetOccupiedPositions(Map map)
    {
        HashSet<Vector2Int> occupied = new();

        foreach (Room room in map.rooms)
        {
            foreach (var e in room.enemies)
                occupied.Add(e.pos);

            foreach (var l in room.loot)
                occupied.Add(l.pos);

            foreach (var o in room.obstacles)
                occupied.Add(o.pos);
        }

        return occupied;
    }

    private static bool TryGetRandomFreeTile(Room room, HashSet<Vector2Int> occupied, out Vector2Int tile)
    {
        int tries = 0;
        do
        {
            tile = room.tiles[Random.Range(0, room.tiles.Count)].pos;
            tries++;
        }
        while (occupied.Contains(tile) && tries < 1000);

        if (tries >= 1000)
        {
            Debug.LogWarning("WARNING, FULLY OCCUPIED ROOM");
            return false;
        }

        return true;
    }

}
