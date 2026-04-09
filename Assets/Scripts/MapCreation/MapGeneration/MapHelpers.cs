using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapHelpers
{
    public static readonly Dictionary<EnemyType, float> EnemyCosts = new()
    {
        { EnemyType.Melee, 0.5f },
        { EnemyType.Ranged, 1f },
        { EnemyType.Bomber, 0.5f },
        { EnemyType.Assassin, 1f },
        { EnemyType.Guardian, 2f }
    };
    public static readonly EnemyType[] EnemyTypes =
    (EnemyType[])Enum.GetValues(typeof(EnemyType));
    public static readonly LootType[] LootTypes =
    (LootType[])Enum.GetValues(typeof(LootType));
    public static readonly ObstacleType[] ObstacleTypes =
    (ObstacleType[])Enum.GetValues(typeof(ObstacleType));

}


[Serializable]
public class Map
{
    public List<Room> rooms = new ();
    public List<RoomConnection> connections = new();

    public Room startRoom;
    public Room endRoom;

    public List<Room> mainPathRooms = new();

    public Map() { }
    public Map(Map other)
    {
        rooms = new List<Room>(other.rooms);
        connections = new List<RoomConnection>(other.connections);
        startRoom = other.startRoom;
        endRoom = other.endRoom;
        mainPathRooms = new List<Room>(other.mainPathRooms);
    }
    public Map Clone() => new Map(this);

    public int chunkCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            foreach (RoomChunk chunk in room.chunks)
            {
                i++;
            }
        }
        return i;
    }
    public int enemyCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.enemies.Count;
        }
        return i;
    }

    public int lootCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.loot.Count;
        }
        return i;
    }

    public int obstacleCount()
    {
        int i = 0;

        foreach (Room room in rooms)
        {
            i += room.obstacles.Count;
        }
        return i;
    }

}
// The finished encounter rooms
[Serializable]
public class Room
{
    public List<RoomChunk> chunks = new();
    public List<(Vector2Int pos, int type)> tiles = new();
    public List<(Vector2Int pos, int type)> enemies = new();
    public List<(Vector2Int pos, int type)> loot = new();
    public List<(Vector2Int pos, int type)> obstacles = new();
    public Vector2Int position;
    public bool onMainPath = false;
    public int orderIndex = 0;
    public float sizeModifier = 0f;
    public float orderModifier = 0f;
    public float enemyBudget = 0f;
    public float enemyBudgetUsed = 0f;
    public float lootBudget = 0f;
    public float lootBudgetUsed = 0f;
    public float obstacleBudget = 0f;
    public float obstacleBudgetUsed = 0f;

    public Vector2Int? entryTile = null;
    public Vector2Int? exitTile = null;

    public Room(Vector2Int pos)
    {
        chunks = new List<RoomChunk>();
        this.position = pos;
    }
}

// The squares the rooms are build from.
[Serializable]
public class RoomChunk
{
    public Vector2Int position;
    public Vector2Int size;

    public int XMin => position.x;
    public int YMin => position.y;
    public int XMax => position.x + size.x - 1;
    public int YMax => position.y + size.y - 1;

    public bool Overlaps(RoomChunk other, int clearance = 1)
    {
        bool xOverlap =
            (XMin - clearance) <= other.XMax &&
            (XMax + clearance) >= other.XMin;

        bool yOverlap =
            (YMin - clearance) <= other.YMax &&
            (YMax + clearance) >= other.YMin;

        return xOverlap && yOverlap;
    }
}

public class RoomConnection
{
    public Room roomA;
    public Room roomB;

    public Vector2Int tileA;
    public Vector2Int tileB;

    public float length;
}

public enum EnemyType
{
    Melee,
    Ranged,
    Bomber,
    Assassin,
    Guardian
}

public enum LootType
{
    Powerup,
    Health
}

public enum TerrainType
{
    Standard,
    Decor1,
    Decor2,
    Path
}

public enum ObstacleType
{
    Spike,
    Pillar
}

public enum WallType
{
    WallSouth = 0,
    WallEast = 1,
    WallNorth = 2,
    WallWest = 3,
    CornerSouth = 4,
    CornerEast = 5,
    CornerNorth = 6,
    CornerWest = 7
}

public enum MapStyle
{
    Ground = 0,
    Farm = 1,
    Forest = 2
}




