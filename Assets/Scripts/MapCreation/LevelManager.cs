using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LevelManager : MonoBehaviour
{

    [SerializeField] MapInstantiator mapInstantiator;

    [SerializeField] TelemetryManager telemetryManager;
    [SerializeField] CircleCollider2D col;
    [SerializeField] SpriteRenderer sprite;
    private StateMachine[] machines;
    public Queue<MapArchiveExporter.MapDTO> finalMaps;
    private List<Vector2> takenGeoBehaviors;
    private List<Vector2> takenEnemBehaviors;
    private List<Vector2> takenFurnBehaviors;

    [SerializeField] GameObject playerPrefab;
    Vector3 _playerSpawnPos;
    Player _player;
    bool _hasSpawnPos;
    bool _inCombat = false;

    bool noMaps = false;
    private void OnEnable() => MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;
    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p) => _player = p;


    void Awake()
    {
        if (mapInstantiator == null)
            mapInstantiator = FindFirstObjectByType<MapInstantiator>();
        
        if (telemetryManager == null)
            telemetryManager = FindFirstObjectByType<TelemetryManager>();

        // ensure collider is a trigger so OnTriggerEnter2D fires
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "enemArchive_maps.json");
        var archive = MapArchiveExporter.LoadArchiveFromJson(path);
        //var archive = MapArchiveExporter.LoadArchiveFromJson("furnArchive_maps.json");
        //var archive = MapArchiveExporter.LoadArchiveFromJson("combArchive_maps.json");
        finalMaps = new Queue<MapArchiveExporter.MapDTO>();
        takenGeoBehaviors = new List<Vector2>();
        takenEnemBehaviors = new List<Vector2>();
        takenFurnBehaviors = new List<Vector2>();

        Shuffle(archive.maps);
        foreach (var map in archive.maps)
        {
            if (finalMaps.Count < 5)
            {

                Vector2 geoBehavior = new Vector2(map.geoBehavior[0], map.geoBehavior[1]);
                Vector2 enemBehavior = new Vector2(map.enemyBehavior[0], map.enemyBehavior[1]);
                Vector2 furnBehavior = new Vector2(map.furnBehavior[0], map.furnBehavior[1]);
                if (map.fitness > 2.75f && !takenGeoBehaviors.Contains(geoBehavior) && !takenEnemBehaviors.Contains(enemBehavior) && !takenFurnBehaviors.Contains(furnBehavior))
                {

                    finalMaps.Enqueue(map);
                    takenGeoBehaviors.Add(geoBehavior);
                    takenEnemBehaviors.Add(enemBehavior);
                    takenFurnBehaviors.Add(furnBehavior);
                }
            }
            else
            {
                break;
            }
        }

        var playMap = finalMaps.Dequeue();
        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(playMap));
        //mapInstantiator.makeTestMap();
        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
        machines = FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
        float[] behaviors = new float[5] {playMap.geoBehavior[0], playMap.furnBehavior[0], playMap.furnBehavior[1], playMap.enemyBehavior[0], playMap.enemyBehavior[1]};
        telemetryManager.SetBehavior(behaviors);
    }

    private void Update()
    {
        bool anyInCombat = false;

        foreach (var m in machines)
        {
            if (m != null && m.GetState() is not IdleState)
            {
                anyInCombat = true;
                break;
            }
        }

        if (anyInCombat == _inCombat) return; // no change -> don't spam enable/disable

        _inCombat = anyInCombat;
        sprite.enabled = !_inCombat;
        col.enabled = !_inCombat;
    }

    void CacheSpawnAndHookPlayer()
    {

        _player = FindFirstObjectByType<Player>();

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
        _player.TeleportTo(_playerSpawnPos);

        var rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        telemetryManager.PlayerDied();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        // Trigger new run
        if (!noMaps)
        {
            telemetryManager.SetPlayerStats(_player.AttackSpeedMultiplier, _player.MovementSpeedMultiplier, _player.DamageMultiplier);
            telemetryManager.UploadData();
        }
        if(finalMaps.Count==0)
        {
            noMaps = true;
        }
        else {
            var playMap = finalMaps.Dequeue();
            mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(playMap));
            machines = FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
            float[] behaviors = new float[5] {playMap.geoBehavior[0], playMap.furnBehavior[0], playMap.furnBehavior[1], playMap.enemyBehavior[0], playMap.enemyBehavior[1]};
            telemetryManager.SetBehavior(behaviors);
        }
        

        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // UnityEngine.Random
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
