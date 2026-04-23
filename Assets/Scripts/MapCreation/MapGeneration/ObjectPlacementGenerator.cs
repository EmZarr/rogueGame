using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.Timeline;
using UnityEngine;
using static UnityEditor.Recorder.OutputPath;

public static class ObjectPlacementGenerator
{
    // How much more loot does optional rooms get
    public const float OptionalLootModifier = 2f;
    // The max ratio of powerups to health we want on main path
    public const float PowerRatioInMain = 0.3f;
    // The minimum ratio of powerups to health we want in optional rooms
    public const float PowerRatioInOptional = 0.6f;

    public const float DefaultMutateSize = 0.2f;

    public static readonly Vector2 DefaultBudgetModifierRange = new(0.5f, 3f);

    // Base budget before modifiers
    public const int DefaultEnemyBaseBudget = 4;
    public const int DefaultLootBaseBudget = 2;
    public const int DefaultObstacleBaseBudget = 3;
    public static Map CreateEnemiesOnMap(Map map)
    {
        return CreateEnemiesOnMap(map, DefaultEnemyBaseBudget, DefaultBudgetModifierRange);
    }

    public static Map CreateEnemiesOnMap(Map map, int baseBudget)
    {
        return CreateEnemiesOnMap(map, baseBudget, DefaultBudgetModifierRange);
    }

    public static Map CreateEnemiesOnMap(Map map, int baseBudget, Vector2 budgetModifierRange)
    {
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
                bool success = PlaceRandomEnemy(room, occupiedPositions, new float[MapHelpers.EnemyTypes.Length]);
                if (!success) break;
            }

        }
        return map;
    }

    public static Map MutateEnemies(Map map)
    {
        return MutateEnemies(map, new float[MapHelpers.EnemyTypes.Length], 0.0f, DefaultMutateSize, DefaultBudgetModifierRange, DefaultEnemyBaseBudget);
    }

    public static Map MutateEnemies(Map map, float[] compBias, float diffBias, float mutateSize, Vector2 budgetModifierRange, int baseBudget)
    {

        // get occupied tiles
        HashSet<Vector2Int> occupiedPositions = GetOccupiedPositions(map);

        // Mutates a few budgets, and adds/removes enemies accordingly
        int budgetsToMutate = Mathf.Max(1, Mathf.RoundToInt(map.rooms.Count * mutateSize));
        for (int i = 0; i < budgetsToMutate; i++)
        {
            Room roomToMutate = map.rooms[Random.Range(0, map.rooms.Count)];
            roomToMutate.enemyBudget = baseBudget
            * roomToMutate.sizeModifier
            * roomToMutate.orderModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y)
            * (1f + diffBias * 0.25f); ; // diff bias is -1 to 1, so now 0.75 to 1.25.

            while (roomToMutate.enemyBudgetUsed > roomToMutate.enemyBudget)
            {
                RemoveRandomEnemy(roomToMutate, occupiedPositions, compBias);
            }
            while (roomToMutate.enemyBudgetUsed < roomToMutate.enemyBudget)
            {
                bool success = PlaceRandomEnemy(roomToMutate, occupiedPositions, compBias);
                if (!success) break;
            }

        }

        // Randomly removes and readds an amount of enemies
        int amountToMutate = Mathf.Max(1, Mathf.RoundToInt(map.enemyCount() * mutateSize));

        for (int i = 0; i < amountToMutate; i++)
        {
            Room roomToMutate =  map.rooms[Random.Range(0, map.rooms.Count)];

            RemoveRandomEnemy(roomToMutate, occupiedPositions, compBias);
            bool success = PlaceRandomEnemy(roomToMutate, occupiedPositions, compBias);
            if (!success) break;
        }
        return map;
    }

    public static Map BiasedMutateEnemies(Map map, float[] compBias, float diffBias)
    {
        return MutateEnemies(map, compBias, diffBias, DefaultMutateSize, DefaultBudgetModifierRange, DefaultEnemyBaseBudget);
    }
    

    public static bool PlaceRandomEnemy(Room room, HashSet<Vector2Int> occupied, float[] compBias)
    {

        // Find Random unoccupied tile
        if (!TryGetRandomFreeTile(room, occupied, out Vector2Int tile)) return false;

        // Choose enemy type randomly, add enemy, update budget and occupied
        EnemyType randomType = GetBiasedRandomEnemy(compBias);
        room.enemies.Add(new GridEntry(tile, (int)randomType));
        room.enemyBudgetUsed += MapHelpers.EnemyCosts[randomType];
        occupied.Add(tile);
        return true;
    }

    public static void RemoveRandomEnemy(Room room, HashSet<Vector2Int> occupied, float[] compBias)
    {
        if (room.enemies == null || room.enemies.Count == 0)
            return;

        float total = 0f;

        // Sum removal chance for each existing enemy instance.
        // Removal uses inverted bias (- instead of plus):
        // positive bias = lower removal chance
        // negative bias = higher removal chance
        for (int i = 0; i < room.enemies.Count; i++)
        {
            EnemyType type = (EnemyType)room.enemies[i].type;
            total += 1f - compBias[(int)type];
        }

        float r = Random.Range(0f, total);

        // Walk along the line until we reach the random point
        // Each subtraction "consumes" one enemy type's portion
        for (int i = 0; i < room.enemies.Count; i++)
        {
            EnemyType type = (EnemyType)room.enemies[i].type;
            r -= 1f - compBias[(int)type];

            if (r <= 0f)
            {
                var enemy = room.enemies[i];

                room.enemies.RemoveAt(i);
                room.enemyBudgetUsed -= MapHelpers.EnemyCosts[type];
                occupied.Remove(enemy.pos);
                return;
            }
        }
    }

    private static EnemyType GetBiasedRandomEnemy(float[] bias)
    {
        int count = MapHelpers.EnemyTypes.Length;

        // Add up the chance for each enemy type.
        // Convert bias (-1 -> 1) into weights (0 -> 2)
        // -1 = never picked, 0 = normal chance, +1 = double chance
        float total = 0f;
        for (int i = 0; i < count; i++)
            total += 1f + bias[i];

        // Pick a random point inside the total weight range
        float r = Random.Range(0f, total);

        // Walk along the line until we reach the random point
        // Each subtraction "consumes" one enemy type's portion
        for (int i = 0; i < count; i++)
        {
            r -= 1f + bias[i];

            // When we cross zero, we've landed in this enemy's "slice"
            if (r <= 0f)
                return MapHelpers.EnemyTypes[i];
        }

        // Fallback
        return MapHelpers.EnemyTypes[count - 1];
    }

    public static Map CreateLootOnMap(Map map)
    {
        return CreateLootOnMap(map, DefaultLootBaseBudget, DefaultBudgetModifierRange);
    }
    public static Map CreateLootOnMap(Map map, int baseBudget, Vector2 budgetModifierRange)
    {
        // Makes a occupied tile list
        HashSet <Vector2Int> occupiedPositions = GetOccupiedPositions(map);
        foreach (Room room in map.rooms)
        {

            // Finds budget based on size, and randomness. More loot for optional
            float roomLootModifier = room.onMainPath ? 1f : OptionalLootModifier;
            room.lootBudget = baseBudget
            * room.sizeModifier
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y)
            * roomLootModifier;
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
        return MutateLoot(map, DefaultMutateSize, DefaultBudgetModifierRange, DefaultLootBaseBudget);
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
            * Random.Range(budgetModifierRange.x, budgetModifierRange.y)
            * OptionalLootModifier;

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

        var ratio = room.GetLootTypeShare(LootType.Powerup);

        // If main room has more powerups than allowed
        if (room.onMainPath && ratio > PowerRatioInMain)
        {
            randomType = LootType.Health;
        }
        // If optional room has less powerups than allowed
        else if (!room.onMainPath && ratio < PowerRatioInOptional) 
        {
            randomType = LootType.Powerup;
        }

        room.loot.Add(new GridEntry(tile, (int)randomType));
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
        return CreateObstaclesOnMap(map, DefaultObstacleBaseBudget, DefaultBudgetModifierRange);
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
        return MutateObstacles(map, DefaultMutateSize, DefaultBudgetModifierRange, DefaultObstacleBaseBudget);
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
        room.obstacles.Add(new GridEntry(tile, (int)randomType));
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
