using UnityEngine;
using UnityEngine.Tilemaps;



public class CreateMap : MonoBehaviour

{
    Tilemap tilemapGround;
    Tilemap tilemapWall;

    [SerializeField] TileBase groundTileGrass1;
    [SerializeField] TileBase groundTileGrass2;
    [SerializeField] TileBase groundTileEarth1;
    [SerializeField] TileBase groundTileEarth2;
    [SerializeField] TileBase wallTile;

    [SerializeField] int sizeX;
    [SerializeField] int sizeY;

    [SerializeField] GameObject health_pickup;
    [SerializeField] GameObject money_pickup;
    [SerializeField] GameObject spike_trap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var list = GetComponentsInChildren<Tilemap>();
        foreach (var elem in list) {
            if (elem.name == "GroundTilemap") {
                tilemapGround = elem;
            }
            else {
                tilemapWall = elem;
            }
            Debug.Log((elem.name.ToString()));
        }
        GenerateMap();
        AddFurnishing();
        
    }

    void GenerateMap()
    {
        Vector3Int test = new Vector3Int(0, 0, 0);
        tilemapGround.ClearAllTiles();
        tilemapWall.ClearAllTiles();
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                test.x = x;
                test.y = y;
                Debug.Log("Map " + x + " " + y);
                if (x == 0 || y == 0 || x == (sizeX - 1) || y == (sizeY - 1))
                {
                    tilemapWall.SetTile(test, wallTile);
                }
                else
                {
                    int type = Random.Range(1, 7);
                    switch (type)
                    {
                        case 1:
                            tilemapGround.SetTile(test, groundTileGrass1);
                            break;
                        case 2:
                            tilemapGround.SetTile(test, groundTileGrass2);
                            break;
                        case 3:
                            tilemapGround.SetTile(test, groundTileEarth1);
                            break;
                        case 4:
                            tilemapGround.SetTile(test, groundTileEarth2);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }


    void AddFurnishing()
    {
        for (int x = 1; x < sizeX-1; x++)
        {
            for (int y = 1; y < sizeY-1; y++)
            {
                var place = Random.Range(1, 11);
                if (place == 10)
                {
                    if (tilemapGround.GetTile(new Vector3Int(x, y, 0)) != null)
                    {
                        Vector3 worldPos = tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0));
                        int type = Random.Range(1, 4);
                        switch (type)
                        {
                            case 1:
                                Instantiate(health_pickup, worldPos, Quaternion.identity);
                                break;
                            case 2:
                                Instantiate(money_pickup, worldPos, Quaternion.identity);
                                break;
                            case 3:
                                Instantiate(spike_trap, worldPos, Quaternion.identity);
                                break;
                        }
                    }
                   
                }
            }
        }
    }
    
    void AddEnemies() 
    {
        
    }
}
