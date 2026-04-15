using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    [SerializeField] MapInstantiator mapInstantiator;
    [SerializeField] TelemetryManager telemetryManager;
    [SerializeField] CircleCollider2D col;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] AutoRecorder autoRecorder;

    private StateMachine[] machines;

    // The active infinite loop qMueue: always 8 maps in fixed order
    public Queue<Map> finalMaps;

    // The exact 8 maps chosen for this run, preserved in order
    public List<Map> playedMaps;

    //Used to save only the maps we need for build
    public List<Map> buildMaps;

    float checkTimer;

    [SerializeField] GameObject playerPrefab;
    Vector3 _playerSpawnPos;
    Player _player;

    Map playMap;

    List<Room> optRooms = new List<Room>();

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
        string path = Path.Combine(Application.streamingAssetsPath, "enemArchive_maps.json");
        //string path = Path.Combine(Application.streamingAssetsPath, "handcrafted_maps.json");
        var archive = MapJsonExporter.LoadMaps(path);

        finalMaps = new Queue<Map>();
        playedMaps = new List<Map>();
        buildMaps = new List<Map>();

        //BuildLevelLoop(archive.maps);
        playedMaps = new List<Map>(archive);
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
            foreach (var room in optRooms)
            {
                if (room.tiles.Any(t => t.pos == playPos))
                {
                    telemetryManager.OptionalRoomEntered();
                    optRooms.Remove(room);
                }
            }

            checkTimer = 0f;
        }

        foreach (var m in machines)
        {
            if (m != null && m.GetState() is not (IdleState or ProtectState))
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

        var loadoutState = _player.GetComponent<LoadoutState>();
        if (loadoutState != null)
        {
            loadoutState.ApplyLoadout(2);
        }
        _player.RefreshUI();

    }

    void HandlePlayerDied(GameObject killer)
    {
        /*var rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }*/
        if (_player == null) return;

        telemetryManager.PlayerDied();
        telemetryManager.SetTotalScore(_player.GetScore());
        telemetryManager.UploadData();
        telemetryManager.ResetStats(true);
        if (autoRecorder != null)
        {
            autoRecorder.RestartRecordingAndSave();
        }

        _player.OnDied -= HandlePlayerDied;
        Destroy(_player.gameObject);
        mapInstantiator.ClearCurrentPlayerReference();
        _player = null;

        ReloadCurrentMap();
        _hasSpawnPos = false;

        StartCoroutine(HookPlayerNextFrame());
    }

    IEnumerator HookPlayerNextFrame()
    {
        yield return null;
        yield return new WaitForSeconds(0.05f);

        CacheSpawnAndHookPlayer();
    }

    void ReloadCurrentMap()
    {
        OptRoomTrig(playMap);
        mapInstantiator.makeMap(playMap);

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
        telemetryManager.SetTotalAmountOfOptionalRooms(optRooms.Count);
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

        //_player.ResetStats();

        _player.OnDied -= HandlePlayerDied;

        mapInstantiator.ClearCurrentPlayerReference();

        Destroy(_player.gameObject);
        _player = null;

        LoadNextMap();

        _hasSpawnPos = false;
        StartCoroutine(HookPlayerNextFrame());
    }

    void BuildLevelLoop(List<Map> archiveMaps)
    {
        playedMaps.Clear();

        // ------------------------------------------------------------
        // INTRO LEVEL (always the same, always first)
        // Replace this rule with your actual intro-level behavior rule
        // ------------------------------------------------------------
        var introRule = new System.Func<Map, bool>(
            map => map.enemyBehavior.y == 1 && (map.enemyBehavior.x == 63) && (map.geoBehavior.x == 1) && (map.furnBehavior.x == 4) && (map.furnBehavior.y == 1) // mix of enemies
        );

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 1 POOL (exactly 4)
        // One of these will be picked randomly
        // ------------------------------------------------------------
        var difficulty1Rules = new List<System.Func<Map, bool>>
        {
            map => map.enemyBehavior.y == 1 && (map.geoBehavior.x == 4) && (map.enemyBehavior.x == 44) && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 0), // small map, no health, no guardian
            map => map.enemyBehavior.y == 1 && (map.geoBehavior.x == 10) && (map.enemyBehavior.x == 50) && (map.furnBehavior.x == 4) && (map.furnBehavior.y == 2), // large, no guardian
        };

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 2 POOL (exactly 8)
        // Three of these will be picked randomly, preserving chosen order
        // ------------------------------------------------------------
        var difficulty2Rules = new List<System.Func<Map, bool>>
        {
            //(geo, enem, furn, map) => enem.y == 2 && geo.x == 2 && (enem.x == 1) && (furn.x == 4) && (furn.y == 1), // All bombers and ranged
            map => map.geoBehavior.x == 3 && map.enemyBehavior.y == 2 && (map.enemyBehavior.x == 5) && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 1), // ranged and guardian, small
            map => map.geoBehavior.x == 19 && map.enemyBehavior.y == 2 && (map.enemyBehavior.x == 51) && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 2), // mainly melee
            map => map.geoBehavior.x == 69 && map.enemyBehavior.y == 2 && (map.enemyBehavior.x == 41) && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 1), // Very large map
            map => map.geoBehavior.x == 31 && (map.enemyBehavior.y == 2) && (map.enemyBehavior.x == 41) && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 4), // almost only health
            map => map.geoBehavior.x == 4 && (map.enemyBehavior.y == 2) && (map.enemyBehavior.x == 38) && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 0), // map
            map => map.geoBehavior.x == 11 && (map.enemyBehavior.y == 2) && (map.enemyBehavior.x == 27) && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 0), // map
            map => map.geoBehavior.x == 12 && (map.enemyBehavior.y == 2) && (map.enemyBehavior.x == 31) && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 2), // map
        };

        // ------------------------------------------------------------
        // HAND-PICKED DIFFICULTY 3 POOL (exactly 8)
        // Three of these will be picked randomly, preserving chosen order
        // ------------------------------------------------------------
        var difficulty3Rules = new List<System.Func<Map, bool>>
        {
            map => map.geoBehavior.x == 69 && map.enemyBehavior.y == 3 && (map.enemyBehavior.x == 41) && (map.furnBehavior.x == 1) && (map.furnBehavior.y == 2), // Very large map,
            map => map.geoBehavior.x == 18 && (map.enemyBehavior.y == 3) && (map.enemyBehavior.x == 41) && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 4), // almost only health
            map => map.geoBehavior.x == 4 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 84 && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 3), // guardian hell
            map => map.geoBehavior.x == 44 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 50 && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 2), // idk cool map
            map => map.geoBehavior.x == 57 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 43 && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 2), // idk cool map
            map => map.geoBehavior.x == 38 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 28 && (map.furnBehavior.x == 2) && (map.furnBehavior.y == 0), // little health (or loot)
            map => map.geoBehavior.x == 16 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 31 && (map.furnBehavior.x == 1) && (map.furnBehavior.y == 1), // idk cool map
            map => map.geoBehavior.x == 7 && map.enemyBehavior.y == 3 && map.enemyBehavior.x == 41 && (map.furnBehavior.x == 3) && (map.furnBehavior.y == 2) // smaller map, many melee
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

    List<Map> BuildPoolFromRules(List<Map> archiveMaps, List<System.Func<Map, bool>> rules, string label)
    {
        var pool = new List<Map>();

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

    Map FindFirstMatchingMap(List<Map> archiveMaps,System.Func<Map, bool> rule)
    {
        foreach (var map in archiveMaps)
        {
            if (rule(map))
                return map;
        }
        return null;
    }

    List<Map> PickRandomUnique(List<Map> pool, int count)
    {
        var copy = new List<Map>(pool);
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

        OptRoomTrig(playMap);
        mapInstantiator.makeMap(playMap);

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
        telemetryManager.SetTotalAmountOfOptionalRooms(optRooms.Count);
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

    // Triggers are optinal rooms that are removed when the player enters them for the first time
    void OptRoomTrig(Map map)
    {
        optRooms.Clear();

        foreach (var room in map.rooms)
        {
            if (!room.onMainPath)
            {
                optRooms.Add(room);
            }
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