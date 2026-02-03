using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInstantiator : MonoBehaviour
{
    [SerializeField] Tilemap tilemapGround;
    [SerializeField] Tilemap tilemapWall;

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

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (map[x, y] == 100)
                {
                }
                switch (map[x, y])
                {
                    case 0:
                        // empty
                        break;

                    case 1:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        break;

                    case 2:
                        tilemapWall.SetTile(cell, wallTile);
                        break;

                    case 3:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        spawnedObjects.Add(
                            Instantiate(furnishingPrefabs[0], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 4:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        spawnedObjects.Add(
                            Instantiate(furnishingPrefabs[1], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 5:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        spawnedObjects.Add(
                            Instantiate(furnishingPrefabs[2], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 6:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        spawnedObjects.Add(
                            Instantiate(enemyPrefabs[0], tilemapGround.GetCellCenterWorld(cell), Quaternion.identity)
                        );
                        break;

                    case 7:
                        tilemapGround.SetTile(cell, groundTileGrass1);

                        int prefabIndex = Random.value < 0.5f ? 1 : 2;
                        prefabIndex = 1;

                        spawnedObjects.Add(
                            Instantiate(
                                enemyPrefabs[prefabIndex],
                                tilemapGround.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        break;

                    case 99:
                        tilemapGround.SetTile(cell, groundTileGrass1);
                        tilemapWall.SetTile(cell, null); //remove wall tile

                        levelManager.transform.position = tilemapGround.GetCellCenterWorld(cell);
                        break;
                    case 100:
                        tilemapGround.SetTile(cell, groundTileGrass1);
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

    }
}
