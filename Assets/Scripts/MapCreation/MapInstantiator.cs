using NavMeshPlus.Components;
using System.Collections.Generic;
using NUnit.Framework;
using System.Drawing;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInstantiator : MonoBehaviour
{

    [SerializeField] Tilemap tilemapGround;
    [SerializeField] Tilemap tilemapWall;

    [SerializeField] TileBase groundTileGrass1;

    [SerializeField] TileBase wallTile;

    [SerializeField] NavMeshSurface surface;

    [SerializeField] GameObject playerPrefab;

    [SerializeField] List<GameObject> enemyPrefabs;

     [SerializeField] List<GameObject> furnishingPrefabs;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void makeMap(int[,] map)
    {
        for (int x = 0; x < map.GetLength(0)-1; x++)
        {
            for (int y = 0; y < map.GetLength(1)-1; y++)
            {
                switch (map[x, y])
                {
                    case 0:
                        break;
                    case 1:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        break;
                    case 2:
                        tilemapWall.SetTile(new Vector3Int(x, y, 0), wallTile);
                        break;
                    case 3:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(furnishingPrefabs[0], tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                    case 4:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(furnishingPrefabs[1], tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                    case 5:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(furnishingPrefabs[2], tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                    case 6:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(enemyPrefabs[0], tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                    case 7:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(enemyPrefabs[1], tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                    case 100:
                        tilemapGround.SetTile(new Vector3Int(x, y, 0), groundTileGrass1);
                        Instantiate(playerPrefab, tilemapGround.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                        break;
                }
            }
        }


        surface.BuildNavMesh();
    }
}
