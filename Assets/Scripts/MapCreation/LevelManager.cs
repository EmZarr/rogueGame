using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LevelManager : MonoBehaviour
{

    [SerializeField] MapInstantiator mapInstantiator;

    [SerializeField] TelemetryManager telemetryManager;
    public Queue<MapArchiveExporter.MapDTO> finalMaps;
    private List<Vector2> takenGeoBehaviors;
    private List<Vector2> takenEnemBehaviors;
    private List<Vector2> takenFurnBehaviors;

    [SerializeField] GameObject playerPrefab;
    Vector3 _playerSpawnPos;
    Player _player;
    bool _hasSpawnPos;
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
        string path = Path.Combine(Application.streamingAssetsPath, "furnArchive_maps.json");
        var archive = MapArchiveExporter.LoadArchiveFromJson(path);
        //var archive = MapArchiveExporter.LoadArchiveFromJson("furnArchive_maps.json");
        //var archive = MapArchiveExporter.LoadArchiveFromJson("combArchive_maps.json");
        finalMaps = new Queue<MapArchiveExporter.MapDTO>();
        takenGeoBehaviors = new List<Vector2>();
        takenEnemBehaviors = new List<Vector2>();
        takenFurnBehaviors = new List<Vector2>();


        foreach (var map in archive.maps)
        {
            if (finalMaps.Count < 5)
            {
                Vector2 geoBehavior = new Vector2(map.geoBehavior[0], map.geoBehavior[1]);
                Vector2 enemBehavior = new Vector2(map.enemyBehavior[0], map.enemyBehavior[1]);
                Vector2 furnBehavior = new Vector2(map.furnBehavior[0], map.furnBehavior[1]);
                finalMaps.Enqueue(map);
                if (map.fitness > 1f && !takenGeoBehaviors.Contains(geoBehavior) && !takenEnemBehaviors.Contains(enemBehavior) && !takenFurnBehaviors.Contains(furnBehavior))
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

        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(finalMaps.Dequeue()));
        //mapInstantiator.makeTestMap();
        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
        telemetryManager.StartTimer();
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
        telemetryManager.UploadData();
        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(finalMaps.Dequeue()));

        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
    }
}
