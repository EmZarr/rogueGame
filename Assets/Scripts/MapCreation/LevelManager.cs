using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [SerializeField] MapInstantiator mapInstantiator;
    [SerializeField] TelemetryManager telemetryManager;
    [SerializeField] CircleCollider2D col;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] AutoRecorder autoRecorder;

    private StateMachine[] machines;

    // The active infinite loop queue: always 8 maps in fixed order
    public Queue<MapArchiveExporter.MapDTO> finalMaps;

    // The exact 8 maps chosen for this run, preserved in order
    public List<MapArchiveExporter.MapDTO> playedMaps;

    //Used to save only the maps we need for build
    public List<MapArchiveExporter.MapDTO> buildMaps;

    float checkTimer;

    [SerializeField] GameObject playerPrefab;
    Vector3 _playerSpawnPos;
    Player _player;

    MapArchiveExporter.MapDTO playMap;

    List<List<Vector2Int>> optComps = new List<List<Vector2Int>>();
    HashSet<Vector2Int> optTiles = new HashSet<Vector2Int>();

    bool _hasSpawnPos;
    bool _inCombat = false;

    bool clearedTutorial = false;

    private void OnEnable() => MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;
    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p) => _player = p;

    public bool IsInCombat() => _inCombat;

    void Awake()
    {
        if (autoRecorder == null)
            autoRecorder = FindFirstObjectByType<AutoRecorder>();
        if (mapInstantiator == null)
            mapInstantiator = FindFirstObjectByType<MapInstantiator>();

        if (telemetryManager == null)
            telemetryManager = FindFirstObjectByType<TelemetryManager>();

        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "buildMapsArchive.json");
        var archive = MapArchiveExporter.LoadArchiveFromJson(path);

        finalMaps = new Queue<MapArchiveExporter.MapDTO>();
        playedMaps = new List<MapArchiveExporter.MapDTO>();
        buildMaps = new List<MapArchiveExporter.MapDTO>();

        BuildLevelLoop(archive.maps);
        RebuildQueueFromPlayedMaps();

        LoadNextMap();

        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
    }

    private void Update()
    {
        bool anyInCombat = false;
        checkTimer += Time.deltaTime;

        if (checkTimer >= 1.0f && _player != null)
        {
            var playPos = new Vector2Int((int)_player.transform.position.x, (int)_player.transform.position.y);

            if (optTiles.Contains(playPos))
            {
                telemetryManager.OptionalComponentEntered();

                foreach (var component in optComps)
                {
                    if (component.Contains(playPos))
                    {
                        foreach (var tile in component)
                        {
                            optTiles.Remove(tile);
                        }
                    }
                }
            }

            checkTimer = 0f;
        }

        foreach (var m in machines)
        {
            if (m != null && m.GetState() is not IdleState)
            {
                anyInCombat = true;
                break;
            }
        }

        if (anyInCombat == _inCombat) return;
        _inCombat = anyInCombat;
        sprite.enabled = !_inCombat;
        col.enabled = !_inCombat;
    }

    void CacheSpawnAndHookPlayer()
    {
        _player = FindFirstObjectByType<Player>();

        if (_player == null) return;

        if (!_hasSpawnPos)
        {
            _playerSpawnPos = _player.transform.position;
            _hasSpawnPos = true;
        }

        _player.OnDied -= HandlePlayerDied;
        _player.OnDied += HandlePlayerDied;
    }

    void HandlePlayerDied(GameObject killer)
    {
        var rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        telemetryManager.PlayerDied();
        telemetryManager.ResetStats(false);
        /*if (autoRecorder != null)
        {
            autoRecorder.DiscardRecording();
        }*/
        // Restart the same chosen 8 maps from the beginning, preserving order
        RebuildQueueFromPlayedMaps();
        _player.ResetStats();
        _player.ResetScore();
        LoadNextMap();
        //_player.ResetStats();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        telemetryManager.SetPlayerStats(
            _player.AttackSpeedMultiplier,
            _player.MovementSpeedMultiplier,
            _player.DamageMultiplier
        );
        _player.IncreaseScore(300);
        telemetryManager.SetTotalScore(_player.GetScore());
        telemetryManager.UploadData();
        machines = FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
        float[] behaviors = new float[5] { playMap.geoBehavior[0], playMap.furnBehavior[0], playMap.furnBehavior[1], playMap.enemyBehavior[0], playMap.enemyBehavior[1] };
        telemetryManager.SetBehavior(behaviors);
        _player.ResetStats();
        
        LoadNextMap();

        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
    }

    void BuildLevelLoop(List<MapArchiveExporter.MapDTO> archiveMaps)
    {
        playedMaps.Clear();

        // ------------------------------------------------------------
        // INTRO LEVEL (always the same, always first)
        // Replace this rule with your actual intro-level behavior rule
        // ------------------------------------------------------------
        var introRule = new System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>(
            (geo, enem, furn, map) => enem.y == 1 && (enem.x == 63) && (geo.x == 1) && (furn.x == 4) && (furn.y == 1) // mix of enemies
        );

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 1 POOL (exactly 4)
        // One of these will be picked randomly
        // ------------------------------------------------------------
        var difficulty1Rules = new List<System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>>
        {
            (geo, enem, furn, map) => enem.y == 1 && (geo.x == 4) && (enem.x == 44) && (furn.x == 2) && (furn.y == 0), // small map, no health, no guardian
            (geo, enem, furn, map) => enem.y == 1 && (geo.x == 10) && (enem.x == 50) && (furn.x == 4) && (furn.y == 2), // large, no guardian
        };

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 2 POOL (exactly 8)
        // Three of these will be picked randomly, preserving chosen order
        // ------------------------------------------------------------
        var difficulty2Rules = new List<System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>>
        {
            //(geo, enem, furn, map) => enem.y == 2 && geo.x == 2 && (enem.x == 1) && (furn.x == 4) && (furn.y == 1), // All bombers and ranged
            (geo, enem, furn, map) => geo.x == 3 && enem.y == 2 && (enem.x == 5) && (furn.x == 3) && (furn.y == 1), // ranged and guardian, small
            (geo, enem, furn, map) => geo.x == 19 && enem.y == 2 && (enem.x == 51) && (furn.x == 3) && (furn.y == 2), // mainly melee
            (geo, enem, furn, map) => geo.x == 69 && enem.y == 2 && (enem.x == 41) && (furn.x == 2) && (furn.y == 1), // Very large map
            (geo, enem, furn, map) => geo.x == 31 && (enem.y == 2) && (enem.x == 41) && (furn.x == 2) && furn.y == 4, // almost only health
            (geo, enem, furn, map) => geo.x == 4 && enem.y == 2 && enem.x == 38 && furn.x == 3 && furn.y == 0, // map
            (geo, enem, furn, map) => geo.x == 11 && enem.y == 2 && enem.x == 27 && furn.x == 2 && furn.y == 0, // map
            (geo, enem, furn, map) => geo.x == 12 && enem.y == 2 && enem.x == 31 && furn.x == 3 && furn.y == 2 // map
        };

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 3 POOL (exactly 8)
        // Three of these will be picked randomly, preserving chosen order
        // ------------------------------------------------------------
        var difficulty3Rules = new List<System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>>
        {
            (geo, enem, furn, map) => geo.x == 69 && enem.y == 3 && (enem.x == 41) && (furn.x == 1) && (furn.y == 2), // Very large map,
            (geo, enem, furn, map) => geo.x == 18 && (enem.y == 3) && (enem.x == 41) && (furn.x == 3) && furn.y == 4, // almost only health
            (geo, enem, furn, map) => geo.x == 4 && enem.y == 3 && enem.x == 84 && (furn.x == 2) && furn.y == 3, // guardian hell
            (geo, enem, furn, map) => geo.x == 44 && enem.y == 3 && enem.x == 50 && furn.x == 2 && furn.y == 2, // idk cool map
            (geo, enem, furn, map) => geo.x == 57 && enem.y == 3 && enem.x == 43 && furn.x == 2 && furn.y == 2, // idk cool map
            (geo, enem, furn, map) => geo.x == 38 && enem.y == 3 && enem.x == 28 && furn.x == 2 && furn.y == 0, // little health (or loot)
            (geo, enem, furn, map) => geo.x == 16 && enem.y == 3 && enem.x == 31 && furn.x == 1 && furn.y == 1, // idk cool map
            (geo, enem, furn, map) => geo.x == 7 && enem.y == 3 && enem.x == 41 && (furn.x == 3) && (furn.y == 2) // smaller map, many melee
        };

        // Pick intro first
        var introMap = FindFirstMatchingMap(archiveMaps, introRule);
        if (introMap == null)
        {
            Debug.LogError("Could not find intro map.");
            return;
        }
        Debug.Log($"Intro map behaviors: geo=({introMap.geoBehavior[0]}, {introMap.geoBehavior[1]}), furn=({introMap.furnBehavior[0]}, {introMap.furnBehavior[1]}), enemy=({introMap.enemyBehavior[0]}, {introMap.enemyBehavior[1]})");
        playedMaps.Add(introMap);
        //buildMaps.Add(introMap);

        // Build pools from hand-picked rules
        var difficulty1Pool = BuildPoolFromRules(archiveMaps, difficulty1Rules, "Difficulty 1");
        var difficulty2Pool = BuildPoolFromRules(archiveMaps, difficulty2Rules, "Difficulty 2");
        var difficulty3Pool = BuildPoolFromRules(archiveMaps, difficulty3Rules, "Difficulty 3");

        //Only used for making a smaller archive with build maps to reduce size
        //MapArchiveExporter.buildMapList(buildMaps, "buildMapsArchive");

        // Pick from each pool
        var pickedDiff1 = PickRandomUnique(difficulty1Pool, 1);
        var pickedDiff2 = PickRandomUnique(difficulty2Pool, 3);
        var pickedDiff3 = PickRandomUnique(difficulty3Pool, 3);

        // Preserve difficulty order in the final loop
        playedMaps.AddRange(pickedDiff1);
        playedMaps.AddRange(pickedDiff2);
        playedMaps.AddRange(pickedDiff3);

        Debug.Log($"Built infinite level loop with {playedMaps.Count} maps.");
    }

    List<MapArchiveExporter.MapDTO> BuildPoolFromRules(
        List<MapArchiveExporter.MapDTO> archiveMaps,
        List<System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>> rules,
        string label)
    {
        var pool = new List<MapArchiveExporter.MapDTO>();

        for (int i = 0; i < rules.Count; i++)
        {
            var map = FindFirstMatchingMap(archiveMaps, rules[i]);

            if (map == null)
            {
                Debug.LogWarning($"{label}: no map found for rule #{i + 1}");
                continue;
            }

            if (pool.Contains(map))
            {
                Debug.LogWarning($"{label}: duplicate map matched for rule #{i + 1}");
                continue;
            }

            pool.Add(map);
            //buildMaps.Add(map);
        }
        return pool;
    }

    MapArchiveExporter.MapDTO FindFirstMatchingMap(
        List<MapArchiveExporter.MapDTO> archiveMaps,
        System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool> rule)
    {
        foreach (var map in archiveMaps)
        {
            var geo = new Vector2Int(
                Mathf.RoundToInt(map.geoBehavior[0]),
                Mathf.RoundToInt(map.geoBehavior[1])
            );

            var enem = new Vector2Int(
                Mathf.RoundToInt(map.enemyBehavior[0]),
                Mathf.RoundToInt(map.enemyBehavior[1])
            );

            var furn = new Vector2Int(
                Mathf.RoundToInt(map.furnBehavior[0]),
                Mathf.RoundToInt(map.furnBehavior[1])
            );

            if (rule(geo, enem, furn, map))
                return map;
        }
        return null;
    }

    List<MapArchiveExporter.MapDTO> PickRandomUnique(List<MapArchiveExporter.MapDTO> pool, int count)
    {
        var copy = new List<MapArchiveExporter.MapDTO>(pool);
        Shuffle(copy);

        if (count > copy.Count)
            count = copy.Count;

        return copy.GetRange(0, count);
    }

    void RebuildQueueFromPlayedMaps()
    {
        finalMaps.Clear();

        foreach (var map in playedMaps)
        {
            finalMaps.Enqueue(map);
        }
    }

    void LoadNextMap()
    {
        if (finalMaps.Count == 0)
        {
            Debug.LogError("No maps available in finalMaps.");
            return;
        }
        //Remove tutorial map from rotation
        if (finalMaps.Count == 7 && !clearedTutorial)
        {
            clearedTutorial = true;
            playedMaps.Remove(playMap);
            //Debug.Log("here");
        }
        //_player.ResetStats();

        // Preserve order while looping infinitely:
        // take front map, play it, then put it at the back
        playMap = finalMaps.Dequeue();
        if(finalMaps.Count < 7)
        {
            finalMaps.Enqueue(playMap);
        }

        FixOptComps(playMap);
        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(playMap));

        StartCoroutine(WaitForEnemies());
        

        float[] behaviors = new float[5]
        {
            playMap.geoBehavior[0],
            playMap.furnBehavior[0],
            playMap.furnBehavior[1],
            playMap.enemyBehavior[0],
            playMap.enemyBehavior[1]
        };
        telemetryManager.SetBehavior(behaviors);
        telemetryManager.SetTotalAmountOfOptionalComponents(playMap.optionalComponents.Count);
    }

    IEnumerator WaitForEnemies()
    {
        yield return new WaitForSeconds(1.5f);
        machines = FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void FixOptComps(MapArchiveExporter.MapDTO map)
    {
        // These collections should only represent the CURRENT map.
        // So yes, clearing them here is correct.
        optTiles.Clear();
        optComps.Clear();

        foreach (var tile in map.optionalComponentTiles)
        {
            var actualTile = mapInstantiator.tilemapBase.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            optTiles.Add(new Vector2Int((int)actualTile.x, (int)actualTile.y));
        }

        foreach (var component in map.optionalComponents)
        {
            var comp = new List<Vector2Int>();

            foreach (var compTile in component.tiles)
            {
                var correctedTile = mapInstantiator.tilemapBase.GetCellCenterWorld(new Vector3Int(compTile.x, compTile.y, 0));
                comp.Add(new Vector2Int((int)correctedTile.x, (int)correctedTile.y));
            }

            optComps.Add(comp);
        }
    }

    public (int geoX, int geoY, int furnX, int furnY, int enemyX, int enemyY) GetCurrentBehaviorTuple()
    {
        if (playMap == null)
            return (0, 0, 0, 0, 0, 0);

        int geoX = Mathf.RoundToInt(playMap.geoBehavior[0]);
        int geoY = 0;

        int furnX = Mathf.RoundToInt(playMap.furnBehavior[0]);
        int furnY = Mathf.RoundToInt(playMap.furnBehavior[1]);

        int enemyX = Mathf.RoundToInt(playMap.enemyBehavior[0]);
        int enemyY = Mathf.RoundToInt(playMap.enemyBehavior[1]);

        return (geoX, geoY, furnX, furnY, enemyX, enemyY);
    }

    public string GetCurrentBehaviorTupleString()
    {
        var t = GetCurrentBehaviorTuple();
        return $"({t.geoX}, {t.geoY}, {t.furnX}, {t.furnY}, {t.enemyX}, {t.enemyY})";
    }
}