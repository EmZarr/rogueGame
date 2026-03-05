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

    public List<MapArchiveExporter.MapDTO> playedMaps;
    private List<Vector2Int> takenGeoBehaviors;
    private List<Vector2Int> takenEnemBehaviors;
    private List<Vector2Int> takenFurnBehaviors;

    float checkTimer;

    [SerializeField] GameObject playerPrefab;
    Vector3 _playerSpawnPos;
    Player _player;

    MapArchiveExporter.MapDTO playMap;

    List<List<Vector2Int>> optComps = new List<List<Vector2Int>>();

    HashSet<Vector2Int> optTiles = new HashSet<Vector2Int>();

    bool _hasSpawnPos;
    bool _inCombat = false;

    bool noMaps = false;
    float minFitness = 2.7f;
    int targetCount = 5;
    private void OnEnable() => MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;
    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p) => _player = p;

    public bool IsInCombat() => _inCombat;


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
        string path = Path.Combine(Application.streamingAssetsPath, "enemTestArchive_maps.json");
        var archive = MapArchiveExporter.LoadArchiveFromJson(path);
        //var archive = MapArchiveExporter.LoadArchiveFromJson("furnArchive_maps.json");
        //var archive = MapArchiveExporter.LoadArchiveFromJson("combArchive_maps.json");
        finalMaps = new Queue<MapArchiveExporter.MapDTO>();
        playedMaps = new List<MapArchiveExporter.MapDTO>();
        takenGeoBehaviors = new List<Vector2Int>();
        takenEnemBehaviors = new List<Vector2Int>();
        takenFurnBehaviors = new List<Vector2Int>();

        //Shuffle(archive.maps);

        var orderedRules = new List<System.Func<Vector2Int, Vector2Int, Vector2Int, MapArchiveExporter.MapDTO, bool>>
        {
            (geo, enem, furn, map) => enem.y == 1,
            (geo, enem, furn, map) => enem.y == 3,
            (geo, enem, furn, map) => geo.x == 1,
            (geo, enem, furn, map) => geo.x == 9,
            (geo, enem, furn, map) => enem.x == 1,

            // You can mix anything:
            // (geo, enem, furn, map) => geo.x == 5,
            // (geo, enem, furn, map) => furn == new Vector2Int(5,0),
            // (geo, enem, furn, map) => enem.x == 126 && geo.y == 0,
        };

        // Apply ordered hard-picks
        for (int r = 0; r < orderedRules.Count && finalMaps.Count < targetCount; r++)
        {
            var rule = orderedRules[r];
            bool found = false;

            foreach (var map in archive.maps)
            {
                if (finalMaps.Count >= targetCount) break;
                if (!CanTake(map, out var geo, out var enem, out var furn)) continue;
                if (!rule(geo, enem, furn, map)) continue;

                Debug.Log($"Ordered pick #{r + 1}: geo={geo}, enem={enem}, furn={furn}, fitness={map.fitness}");
                Take(map, geo, enem, furn);
                found = true;
                break; //exactly one map per ordered rule
            }

            if (!found)
                Debug.LogWarning($"No eligible map found for ordered rule #{r + 1}.");
        }
        // Fill rest
        foreach (var map in archive.maps)
        {
            if (finalMaps.Count >= targetCount) break;

            if (!CanTake(map, out var geo, out var enem, out var furn)) continue;
            Take(map, geo, enem, furn);
        }
        playMap = finalMaps.Dequeue();
        FixOptComps(playMap);
        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(playMap));
        _hasSpawnPos = false;
        CacheSpawnAndHookPlayer();
        machines = FindObjectsByType<StateMachine>(FindObjectsSortMode.None);
        float[] behaviors = new float[5] {playMap.geoBehavior[0], playMap.furnBehavior[0], playMap.furnBehavior[1], playMap.enemyBehavior[0], playMap.enemyBehavior[1]};
        telemetryManager.SetBehavior(behaviors);
        telemetryManager.SetTotalAmountOfOptionalComponents(playMap.optionalComponents.Count);
    }

    private void Update()
    {
        bool anyInCombat = false;
        checkTimer += Time.deltaTime;
        if(checkTimer >= 1.0f)
        {
            var playPos = new Vector2Int((int)_player.transform.position.x, (int)_player.transform.position.y);
            if (optTiles.Contains(playPos))
            {
                telemetryManager.OptionalComponentEntered();
                Debug.Log(playPos);
                foreach(var component in optComps)
                {
                    if (component.Contains(playPos))
                    {
                        foreach(var tile in component)
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
        //_player.TeleportTo(_playerSpawnPos);

        var rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        telemetryManager.PlayerDied();
        telemetryManager.UploadData();
        //telemetryManager.ResetStats(false);
        finalMaps.Clear();
        foreach(var map in playedMaps)
        {
            finalMaps.Enqueue(map);
        }
        //finalMaps = playedMaps;
        Debug.Log("Played maps: " + playedMaps.Count);
        Debug.Log("Final maps: " + finalMaps.Count);
        playMap = finalMaps.Dequeue();
        FixOptComps(playMap);
        mapInstantiator.makeMap(MapArchiveExporter.MapFromDto(playMap));
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
            playMap = finalMaps.Dequeue();
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

    bool CanTake(MapArchiveExporter.MapDTO map, out Vector2Int geo, out Vector2Int enem, out Vector2Int furn)
    {
        geo = new Vector2Int(Mathf.RoundToInt(map.geoBehavior[0]), Mathf.RoundToInt(map.geoBehavior[1]));
        enem = new Vector2Int(Mathf.RoundToInt(map.enemyBehavior[0]), Mathf.RoundToInt(map.enemyBehavior[1]));
        furn = new Vector2Int(Mathf.RoundToInt(map.furnBehavior[0]), Mathf.RoundToInt(map.furnBehavior[1]));

        if (map.fitness <= minFitness) return false;

        if (takenGeoBehaviors.Contains(geo)) return false;
        if (takenEnemBehaviors.Contains(enem)) return false;
        if (takenFurnBehaviors.Contains(furn)) return false;

        return true;
    }

    void Take(MapArchiveExporter.MapDTO map, Vector2Int geo, Vector2Int enem, Vector2Int furn)
    {
        finalMaps.Enqueue(map);
        playedMaps.Add(map);
        takenGeoBehaviors.Add(geo);
        takenEnemBehaviors.Add(enem);
        takenFurnBehaviors.Add(furn);
    }

    void FixOptComps(MapArchiveExporter.MapDTO map)
    {
        foreach(var tile in map.optionalComponentTiles)
        {
            var actualTile = mapInstantiator.tilemapBase.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            optTiles.Add(new Vector2Int((int)actualTile.x, (int)actualTile.y));
        }
        foreach(var component in map.optionalComponents)
        {
            var comp = new List<Vector2Int>();
            foreach(var compTile in component.tiles)
            {
                var correctedTile = mapInstantiator.tilemapBase.GetCellCenterWorld(new Vector3Int(compTile.x, compTile.y, 0));
                comp.Add(new Vector2Int((int)correctedTile.x, (int)correctedTile.y));
            }
            optComps.Add(comp);
        }
    }
}
