using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInstantiator : MonoBehaviour
{
    [SerializeField] Tilemap tilemapBase;
    [SerializeField] Tilemap tilemapRoad;
    [SerializeField] Tilemap tilemapDecour;
    [SerializeField] Tilemap tilemapWall;

    [SerializeField] TileBase[] tilesRoad;


    [Header ("Base")]
    [SerializeField] TileBase[] tilesGroundBase;
    [SerializeField] TileBase[] tilesFarmBase;
    [SerializeField] TileBase[] tilesForestBase;

    [Header ("Decour")]
    [SerializeField] TileBase[] tilesGroundDecour;
    [SerializeField] TileBase[] tilesFarmDecour;
    [SerializeField] TileBase[] tilesForestDecour;

    [Header ("Walls")]
    [SerializeField] TileBase[] tilesGroundWalls;
    [SerializeField] TileBase[] tilesFarmWalls;
    [SerializeField] TileBase[] tilesForestWalls;

    [Header ("Roads")]
    [SerializeField] TileBase[] tilesGroundRoads;
    [SerializeField] TileBase[] tilesFarmRoads;
    [SerializeField] TileBase[] tilesForestRoads;


    [SerializeField] TileBase[] tilesCorner;

    [SerializeField] TileBase[] tilesWall;



    [SerializeField] TileBase groundTileGrass1;
    [SerializeField] TileBase wallTile;
    [SerializeField] TileBase testTile;

    [SerializeField] NavMeshSurface surface;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] List<GameObject> enemyPrefabs;
    [SerializeField] List<GameObject> furnishingPrefabs;

    [SerializeField] GameObject levelManager;

    // keep references to everything we spawn so we can delete them later
    private List<GameObject> spawnedObjects = new List<GameObject>();

    public void makeMap(int[,] map)
    {
        ClearPreviousMap();
        int tileIndex;
        int mapStyle = Random.Range(0,3);
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                var cell = new Vector3Int(x, y, 0);
                switch (map[x, y])
                {
                    case 0:
                        // empty
                        break;

                    case 1:
                        int type = Random.Range(0,5);
                        int decour = Random.Range(0,40);
                        switch (mapStyle)
                        {
                            
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[type]);
                                if (decour < 5)
                                {
                                    tilemapDecour.SetTile(cell, tilesGroundDecour[decour]);
                                }
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[type]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[type]);
                                break;
                        }
                        break;
                    case 2:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapRoad.SetTile(cell, tilesGroundRoads[0]);
                                break;
                            case 1:
                                tilemapRoad.SetTile(cell, tilesFarmRoads[0]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapRoad.SetTile(cell, tilesForestRoads[0]);
                                break;
                        }
                        break;

                    case 3:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[0]);;
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[0]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[0]);
                                //tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        break;

                    case 4:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[1]);;
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[1]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[1]);
                                //tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        break;

                    case 5:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[2]);;
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[2]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[2]);
                                break;
                        }
                        break;

                    case 6:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(enemyPrefabs[0], tilemapBase.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 7:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[1],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 8:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[2],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 9:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[3],
                               tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                   
                    case 10:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[4],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 11:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[0],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 12:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[1],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 13:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[2],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 14:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[3],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 15:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[4],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 16:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[5],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 17:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[6],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 25:
                        tilemapBase.SetTile(cell, tilesGroundDecour[0]);
                        break;
                    case 26:
                        tilemapBase.SetTile(cell, tilesGroundDecour[2]);
                        break;
                    case 27:
                        tilemapBase.SetTile(cell, tilesGroundDecour[14]);
                        break;
                    case 31:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[0]);;
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[0]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[0]);
                                break;
                        }
                        break;
                    case 32:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[1]);;
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[1]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[1]);
                                break;
                        }
                        break;

                    case 99:
                        tileIndex = Random.Range(0,3);
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        tilemapWall.SetTile(cell, null); //remove wall tile

                        levelManager.transform.position = tilemapBase.GetCellCenterWorld(cell);
                        break;
                    case 100:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[0]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[0]);
                                break;
                        }
                        spawnedObjects.Add(
                            Instantiate(playerPrefab, tilemapBase.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;
                    case 98:
                        tilemapBase.SetTile(cell, testTile);
                        break;
                    
                }
            }
        }

        StartCoroutine(BuildNavmeshNextFrame());
    }

    IEnumerator BuildNavmeshNextFrame()
    {
        
        yield return null; // need to wait a frame for the tilemaps to update
        surface.BuildNavMesh();
    }

    void ClearPreviousMap()
    {

        foreach (var go in spawnedObjects)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedObjects.Clear();

        tilemapWall.ClearAllTiles();
        tilemapBase.ClearAllTiles();
        tilemapRoad.ClearAllTiles();
        tilemapDecour.ClearAllTiles();

    }

    public void makeTestMap()
    {
        int[,] map = new int[6,6];
        //map[0,0] = 5;
        map[0,1] = 3;
        map[0,2] = 4;
        map[1,0] = 4;
        map[1,1] = 1;
        map[1,2] = 4;
        map[2,1] = 1;
        map[2,0] = 4;
        map[2,2] = 1;
        map[3,2] = 1;
        map[3,1] = 3;
        map[3,0] = 3;
        map[3,3] = 1;
        map[4,3] = 1;
        //map[0,2] = 4;
        makeMap(map);
    }
}
