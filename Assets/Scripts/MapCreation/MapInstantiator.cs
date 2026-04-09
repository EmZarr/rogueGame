using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInstantiator : MonoBehaviour
{
    [SerializeField] public Tilemap tilemapBase;
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

    [SerializeField] private MapSurroundingsGenerator surroundingsGenerator;

    public static Player CurrentPlayer { get; private set; }
    public static System.Action<Player> OnPlayerSpawned;

    [SerializeField] public TelemetryManager telemetryManager;

    // keep references to everything we spawn so we can delete them later
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedLoot = new List<GameObject>();

    private List<GameObject> spawnedObjects = new List<GameObject>();

    private List<(GameObject, Vector3)> enemiesToSpawn = new List<(GameObject, Vector3)>();


    void Start()
    {
        telemetryManager = FindAnyObjectByType<TelemetryManager>();
    }
    public void makeMap(int[,] map)
    {
        ClearPreviousMap();
        telemetryManager.IncreaseTotalMapScore(300);
        int tileIndex;
        //int mapStyle = Random.Range(0,3);
        int mapStyle = 1;
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
                                tilemapWall.SetTile(cell, tilesGroundWalls[3]);
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[3]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[3]);
                                break;
                        }
                        break;

                    case 4:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[0]);
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[0]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[0]);
                                break;
                        }
                        break;

                    case 5:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[1]);
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[1]);
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[1]);
                                break;
                        }
                        break;

                    case 6:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[2]);
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

                    case 7:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[6]); // B11_N
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[6]);   // B11_N
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[6]); // B11_N
                                break;
                        }
                        break;

                    case 8:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[7]); // B11_W
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[7]);   // B11_W
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[7]); // B11_W
                                break;
                        }
                        break;

                    case 9:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[4]); // B11_S
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[4]);   // B11_S
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[4]); // B11_S
                                break;
                        }
                        break;

                    case 10:
                        switch (mapStyle)
                        {
                            case 0:
                                tilemapWall.SetTile(cell, tilesGroundWalls[5]); // B11_E
                                break;
                            case 1:
                                tilemapWall.SetTile(cell, tilesFarmWalls[5]);   // B11_E
                                tilemapBase.SetTile(cell, tilesGroundBase[2]);
                                break;
                            case 2:
                                tilemapWall.SetTile(cell, tilesForestWalls[5]); // B11_E
                                break;
                        }
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
                        var fixPos = tilemapBase.GetCellCenterWorld(cell);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[0],
                                new Vector3(fixPos.x, fixPos.y, fixPos.z),
                                Quaternion.identity
                            )
                        );
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[1],
                                new Vector3(fixPos.x, fixPos.y, fixPos.z),
                                Quaternion.identity
                            )
                        );
                        
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[2],
                                new Vector3(fixPos.x-0.1f, fixPos.y-0.1f, fixPos.z),
                                Quaternion.identity
                            )
                        );
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[3],
                                new Vector3(fixPos.x, fixPos.y, fixPos.z),
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
                        var fixPos2 = tilemapBase.GetCellCenterWorld(cell);
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[0],
                                new Vector3(fixPos2.x, fixPos2.y, fixPos2.z),
                                Quaternion.identity
                            )
                        );
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[1],
                                new Vector3(fixPos2.x, fixPos2.y, fixPos2.z),
                                Quaternion.identity
                            )
                        );
                        
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[2],
                                new Vector3(fixPos2.x-0.1f, fixPos2.y-0.1f, fixPos2.z),
                                Quaternion.identity
                            )
                        );
                        spawnedObjects.Add(
                            Instantiate(
                                furnishingPrefabs[3],
                                new Vector3(fixPos2.x, fixPos2.y, fixPos2.z),
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
                        spawnedLoot.Add(
                            Instantiate(
                                furnishingPrefabs[4],
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
                        spawnedLoot.Add(
                            Instantiate(
                                furnishingPrefabs[5],
                                tilemapBase.GetCellCenterWorld(cell),
                                Quaternion.identity
                            )
                        );
                        telemetryManager.IncreaseTotalMapScore(30);
                        telemetryManager.IncreaseMapPowerupCounter();
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
                    case 40:
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
                        enemiesToSpawn.Add((enemyPrefabs[0], tilemapBase.GetCellCenterWorld(cell)));
                        telemetryManager.IncreaseTotalMapScore(20);
                        break;
                    case 41:
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
                        enemiesToSpawn.Add((enemyPrefabs[1], tilemapBase.GetCellCenterWorld(cell)));
                        telemetryManager.IncreaseTotalMapScore(30);
                        break;
                    case 42:
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
                        enemiesToSpawn.Add((enemyPrefabs[2], tilemapBase.GetCellCenterWorld(cell)));
                        telemetryManager.IncreaseTotalMapScore(30);
                        break;
                    case 43:
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
                        enemiesToSpawn.Add((enemyPrefabs[3], tilemapBase.GetCellCenterWorld(cell)));
                        telemetryManager.IncreaseTotalMapScore(30);
                        break;
                    case 44:
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
                        enemiesToSpawn.Add((enemyPrefabs[4], tilemapBase.GetCellCenterWorld(cell)));
                        telemetryManager.IncreaseTotalMapScore(50);
                        break;
                    case 55:
                        int type2 = Random.Range(0,5);
                        switch (mapStyle)
                        {
                            
                            case 0:
                                tilemapBase.SetTile(cell, tilesGroundBase[type2]);
                                break;
                            case 1:
                                tilemapBase.SetTile(cell, tilesFarmBase[type2]);
                                break;
                            case 2:
                                tilemapBase.SetTile(cell, tilesForestBase[type2]);
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
                        {
                            // Ensure base tile
                            switch (mapStyle)
                            {
                                case 0: tilemapBase.SetTile(cell, tilesGroundBase[0]); break;
                                case 1: tilemapBase.SetTile(cell, tilesFarmBase[0]); break;
                                case 2: tilemapBase.SetTile(cell, tilesForestBase[0]); break;
                            }

                            Vector3 spawnPos = tilemapBase.GetCellCenterWorld(cell);

                            if (CurrentPlayer == null)
                            {
                                var playerGO = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

                                CurrentPlayer = playerGO.GetComponent<Player>();
                                OnPlayerSpawned?.Invoke(CurrentPlayer);
                            }
                            else
                            {
                                CurrentPlayer.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                            }

                            break;
                        }
                    case 98:
                        tilemapBase.SetTile(cell, testTile);
                        break;
                    
                }
            }
        }
        if (surroundingsGenerator != null)
        {
            surroundingsGenerator.GenerateSurroundings(map);
        }

        if (telemetryManager == null)
        {
            telemetryManager = GetComponent<TelemetryManager>();
        }
        
        telemetryManager.SetTotalLoot(spawnedLoot.Count);
        StartCoroutine(BuildNavmeshNextFrame());
        telemetryManager.StartTimer();
    }

    IEnumerator BuildNavmeshNextFrame()
    {
        
        yield return null; // need to wait a frame for the tilemaps to update
        surface.BuildNavMesh();
        spawnEnemies();
    }

    void spawnEnemies()
    {
        foreach (var (enemy, pos) in enemiesToSpawn)
        {
            spawnedEnemies.Add(Instantiate(enemy, pos, Quaternion.identity));
        }
        telemetryManager.SetTotalEnemies(spawnedEnemies.Count);
    }

    public void ClearCurrentPlayerReference()
    {
        CurrentPlayer = null;
    }

    void ClearPreviousMap()
    {

        foreach (var go in spawnedObjects)
        {
            if (go != null)
                Destroy(go);
        }
        foreach (var go in spawnedEnemies)
        {
            if (go != null)
                Destroy(go);
        }
        foreach (var go in spawnedLoot)
        {
            if (go != null)
                Destroy(go);
        }

        spawnedObjects.Clear();
        spawnedEnemies.Clear();
        spawnedLoot.Clear();
        enemiesToSpawn.Clear();

        tilemapWall.ClearAllTiles();
        tilemapBase.ClearAllTiles();
        tilemapRoad.ClearAllTiles();
        tilemapDecour.ClearAllTiles();

        if (surroundingsGenerator != null)
        {
            surroundingsGenerator.ClearSurroundings();
        }

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
