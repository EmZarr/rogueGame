using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Style")]
    [SerializeField] MapStyle mapStyle = MapStyle.Farm;
    TileBase[] GetBaseTiles() => mapStyle == MapStyle.Ground ? tilesGroundBase : mapStyle == MapStyle.Forest ? tilesForestBase : tilesFarmBase;
    TileBase[] GetWallTiles() => mapStyle == MapStyle.Ground ? tilesGroundWalls : mapStyle == MapStyle.Forest ? tilesForestWalls : tilesFarmWalls;
    TileBase[] GetRoadTiles() => mapStyle == MapStyle.Ground ? tilesGroundRoads : mapStyle == MapStyle.Forest ? tilesForestRoads : tilesFarmRoads;


    void Start()
    {
        telemetryManager = FindAnyObjectByType<TelemetryManager>();
    }

    public void makeMap(Map map)
    {
        ClearPreviousMap();
        telemetryManager.IncreaseTotalMapScore(300);

        // Build floor set once upfront for wall neighbor checks
        var floorTiles = new HashSet<Vector2Int>(
            map.rooms.SelectMany(r => r.tiles.Select(t => t.pos))
        );

        // Paint floor tiles
        foreach (var room in map.rooms)
        {
            foreach (var (pos, type) in room.tiles)
            {
                var cell = new Vector3Int(pos.x, pos.y, 0);

                switch ((TerrainType)type)
                {
                    case TerrainType.Standard:
                        tilemapBase.SetTile(cell, GetBaseTiles()[Random.Range(0, 5)]);
                        break;

                    case TerrainType.Path:
                        tilemapBase.SetTile(cell, GetBaseTiles()[Random.Range(0, 5)]);
                        tilemapRoad.SetTile(cell, GetRoadTiles()[0]);
                        break;

                    case TerrainType.Decor1:
                        tilemapBase.SetTile(cell, tilesGroundDecour[0]);
                        break;

                    case TerrainType.Decor2:
                        tilemapBase.SetTile(cell, tilesGroundDecour[2]);
                        break;
                }
            }

            //place obstacles
            foreach (var (pos, type) in room.obstacles)
            {
                var cell = new Vector3Int(pos.x, pos.y, 0);
                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                tilemapWall.SetTile(cell, tilesFarmWalls[GetWallIndex(pos, floorTiles)]);
            }

            //Place loot 
            foreach (var (pos, type) in room.loot)
            {
                var cell = new Vector3Int(pos.x, pos.y, 0);
                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                spawnedLoot.Add(Instantiate(furnishingPrefabs[(int)type], tilemapBase.GetCellCenterWorld(cell), Quaternion.identity));
            }

            // Queue enemies for spawning
            foreach (var (pos, type) in room.enemies)
            {
                var cell = new Vector3Int(pos.x, pos.y, 0);
                tilemapBase.SetTile(cell, tilesFarmBase[0]);
                enemiesToSpawn.Add((enemyPrefabs[(int)type], tilemapBase.GetCellCenterWorld(cell)));
            }
        }

        // paint walls
        foreach (var pos in new HashSet<Vector2Int>(floorTiles))
        {
            var neighbors = new[]
            {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };

            foreach (var n in neighbors)
            {
                if (floorTiles.Contains(n)) continue;

                var cell = new Vector3Int(n.x, n.y, 0);
                tilemapWall.SetTile(cell, GetWallTiles()[GetWallIndex(n, floorTiles)]);
            }
        }

        // spawn player at first tile
        if (map.startRoom?.entryTile != null)
        {
            var cell = new Vector3Int(map.startRoom.entryTile.Value.x, map.startRoom.entryTile.Value.y, 0);
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
        }

        // Place portal at end room exit tile
        if (map.endRoom?.exitTile != null)
        {
            var cell = new Vector3Int(map.endRoom.exitTile.Value.x, map.endRoom.exitTile.Value.y, 0);
            tilemapWall.SetTile(cell, null);
            levelManager.transform.position = tilemapBase.GetCellCenterWorld(cell);
        }

        // make surroundings
        if (surroundingsGenerator != null)
            surroundingsGenerator.GenerateSurroundings(map);

        // telemtry part
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

    int GetWallIndex(Vector2Int pos, HashSet<Vector2Int> floorTiles)
    {
        bool up = floorTiles.Contains(pos + Vector2Int.up);
        bool down = floorTiles.Contains(pos + Vector2Int.down);
        bool left = floorTiles.Contains(pos + Vector2Int.left);
        bool right = floorTiles.Contains(pos + Vector2Int.right);

        if (left && down) return (int)WallType.CornerSouth;
        if (down && right) return (int)WallType.CornerEast;
        if (right && up) return (int)WallType.CornerNorth;
        if (up && left) return (int)WallType.CornerWest;

        if (up) return (int)WallType.WallSouth;
        if (left) return (int)WallType.WallEast;
        if (down) return (int)WallType.WallNorth;
        if (right) return (int)WallType.WallWest;

        return (int)WallType.WallWest; ; // random wall for fallback
    }


}
