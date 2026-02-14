using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInstantiator : MonoBehaviour
{
    [SerializeField] Tilemap tilemapGroundBase;

    [SerializeField] Tilemap tilemapRoad;
    [SerializeField] Tilemap tilemapGroundDecour;
    [SerializeField] Tilemap tilemapGround;
    [SerializeField] Tilemap tilemapWall;

    [SerializeField] TileBase[] tilesRoad;

    [SerializeField] TileBase[] tilesGroundBase;

    [SerializeField] TileBase[] tilesFarm;

    [SerializeField] TileBase[] tilesForest;

    [SerializeField] TileBase[] tilesGround;

    [SerializeField] TileBase[] tilesGroundDecour;

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
                        /*tileIndex = Random.Range(0,7);
                        if (tileIndex < 3)
                        {
                            tilemapGround.SetTile(cell, tilesFarm[tileIndex]);
                        }
                        else
                        {
                            tilemapGroundBase.SetTile(cell, tilesFarm[tileIndex]);
                        }*/
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        break;
                    case 2:
                        tileIndex = Random.Range(0,3);
                        tilemapRoad.SetTile(cell, tilesRoad[0]);
                        break;

                    case 3:
                        tileIndex = Random.Range(0,7);
                        tilemapWall.SetTile(cell, tilesWall[0]);
                        break;

                    case 4:
                        tilemapWall.SetTile(cell, tilesWall[1]);
                        /*spawnedObjects.Add(
                            Instantiate(furnishingPrefabs[5], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );*/
                        break;

                    case 5:
                        tilemapWall.SetTile(cell, tilesCorner[0]);
                        break;

                    case 6:
                        /*tileIndex = Random.Range(0,7);
                        if (tileIndex < 3)
                        {
                            tilemapGround.SetTile(cell, tilesFarm[tileIndex]);
                        }
                        else
                        {
                            tilemapGroundBase.SetTile(cell, tilesFarm[tileIndex]);
                        }*/
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(enemyPrefabs[0], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 7:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[1],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 8:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[2],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 9:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[3],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                   
                    case 10:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[4],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 11:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[0],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 12:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[1],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 13:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[2],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 14:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[3],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 15:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[4],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 16:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[5],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;
                    case 17:
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[6],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;

                    case 99:
                        tileIndex = Random.Range(0,3);
                        tilemapGround.SetTile(cell, tilesGroundBase[0]);
                        tilemapWall.SetTile(cell, null); //remove wall tile

                        levelManager.transform.position = tilemapGround.GetCellCenterWorld(cell);
                        break;
                    case 100:
                       /*tileIndex = Random.Range(0,7);
                        if (tileIndex < 3)
                        {
                            tilemapGround.SetTile(cell, tilesFarm[tileIndex]);
                        }
                        else
                        {
                            tilemapGroundBase.SetTile(cell, tilesFarm[tileIndex]);
                        }*/
                        tilemapGroundBase.SetTile(cell, tilesGroundBase[0]);
                        spawnedObjects.Add(
                            Instantiate(playerPrefab, tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;
                    case 98:
                        tilemapGround.SetTile(cell, testTile);
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

        tilemapGround.ClearAllTiles();
        tilemapWall.ClearAllTiles();
        tilemapGroundBase.ClearAllTiles();
        tilemapRoad.ClearAllTiles();
        tilemapGroundDecour.ClearAllTiles();

    }
}
