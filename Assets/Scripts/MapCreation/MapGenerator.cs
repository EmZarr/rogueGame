using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] Vector3Int mapSize;
    [SerializeField] Vector3Int maxOutLineSize;
    [SerializeField] Vector3Int maxRoomSize;

    [SerializeField] int maxRoomAmount;

    public int[,] mapArray;
    private List<(Vector3Int placement,Vector3Int size)> rooms = new List<(Vector3Int,Vector3Int)>();

    MapInstantiator mapInstantiator;

    void Awake()
    {
        mapArray = new int[mapSize.x, mapSize.y];
    }

    void Start()
    {
        mapInstantiator = FindFirstObjectByType<MapInstantiator>();
        makeRoomGeometry();
        mapInstantiator.makeMap(mapArray);
    }

    void makeRoomGeometry()
    {
        Vector3Int outlinePlacement = new Vector3Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y), 0);
        Vector3Int outlineSize = new Vector3Int(Random.Range(1, maxOutLineSize.x), Random.Range(1, maxOutLineSize.y), 0);
        
        while (!checkRoomPlacementConstraint(outlinePlacement, outlineSize, mapSize))
        {
            outlinePlacement = new Vector3Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y), 0);
            outlineSize = new Vector3Int(Random.Range(1, maxOutLineSize.x), Random.Range(1, maxOutLineSize.y), 0);
        }
        int roomAmount = Random.Range(1, maxRoomAmount);
        for (int i = 0; i<roomAmount; i++)
        {
            Vector3Int roomPlacement = new Vector3Int(Random.Range(outlinePlacement.x, outlinePlacement.x+outlineSize.x), Random.Range(outlinePlacement.y, outlinePlacement.y+outlineSize.y), 0);
            Vector3Int roomSize = new Vector3Int(Random.Range(1, maxRoomSize.x), Random.Range(1, maxRoomSize.y), 0);

            while (!checkRoomPlacementConstraint(roomPlacement, roomSize, outlinePlacement+outlineSize))
            {
                roomPlacement = new Vector3Int(Random.Range(outlinePlacement.x, outlinePlacement.x+outlineSize.x), Random.Range(outlinePlacement.y, outlinePlacement.y+outlineSize.y), 0);
                roomSize = new Vector3Int(Random.Range(1, maxRoomSize.x), Random.Range(1, maxRoomSize.y), 0);
            }
            rooms.Add((roomPlacement, roomSize));
        }
        

        for (int x = 0; x<mapSize.x; x++)
        {
            for (int y = 0; y<mapSize.y; y++)
            {
                mapArray[x, y] = 0; // empty
                if (x >= outlinePlacement.x && x < outlinePlacement.x + outlineSize.x &&
                         y >= outlinePlacement.y && y < outlinePlacement.y + outlineSize.y){
                    mapArray[x, y] = 2; // Wall
                }
                foreach (var room in rooms)
                {
                    if (x >= room.placement.x && x < room.placement.x + room.size.x &&
                    y >= room.placement.y && y < room.placement.y + room.size.y)
                    {
                        mapArray[x, y] = 1; // floor
                    }
                } 
            }
        }
        
    }

    bool checkRoomPlacementConstraint(Vector3Int placement, Vector3Int size, Vector3Int mapSize)
    {
        if (placement.x + size.x > mapSize.x || placement.y + size.y > mapSize.y)
        {
            Debug.Log("Here?");
            return false;
        }
        return true;
    }
}
