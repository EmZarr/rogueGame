using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapHelpers
{
    
}


[Serializable]
public class Map
{
    public List<Room> rooms = new ();
    public List<RoomConnection> connections = new();

    public Room startRoom;
    public Room endRoom;

    public List<Room> mainPathRooms = new();
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




