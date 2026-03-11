using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] public EnemyData _data;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private IAttack _attack;
    [SerializeField] private float maxDashLenght;
    [SerializeField] private float dashCooldown = 3.0f;
    [SerializeField] private DamageFlash damageFlash;
    [SerializeField] private Collider2D enemyCollider;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

    public bool dashAttacking = false;

    [SerializeField] private EnemyAnimDriver animDriver;

    private Player _player;

    private TelemetryManager telemetryManager;

    private Action onDeathEffect = null;
    private bool _dying = false;
    private float _nextDashTime;
    public float _currentHealth;
    public float RemainingStunDuration { get; private set; }
    public bool IsStunned => RemainingStunDuration > 0f;

    public bool attacking = false;
    public bool canDash =>  Time.time > _nextDashTime;

    public bool canProtect {get; set;}

    public Vector3 HomePosition { get; private set; }
    public float WanderRadius => _data.wanderRadius;
    public Vector2 WanderWaitRange => _data.wanderWaitRange;
    bool _dead;

    PopUpCreator popup;

    private void OnEnable()
    {
        MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;

        // catch up in case player already spawned
        if (_player == null)
            HandlePlayerSpawned(MapInstantiator.CurrentPlayer);
        popup = FindFirstObjectByType<PopUpCreator>();
    }
    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p) => _player = p;
    private void Awake()
    {
        _currentHealth = _data.health;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        HomePosition = transform.position;
        _nextDashTime = Time.time;
        canProtect = true;
        RemainingStunDuration = 0f;
        telemetryManager = FindAnyObjectByType<TelemetryManager>();
    }

    private void Update()
    {
        // For combat back
        if (RemainingStunDuration > 0f)
        {
            RemainingStunDuration -= Time.deltaTime;
        }
        if (canDash && maxDashLenght > 0f && !_dying && !IsStunned && _player != null && !(_data.enemyType == EnemyType.Assassin && attacking))
        {
            int mask = LayerMask.GetMask("PlayerAttack", "Player");
            Vector2 enemyPos = transform.position;
            float radius = 3f;

            Collider2D[] hits = Physics2D.OverlapCircleAll(enemyPos, radius, mask);

            foreach (var h in hits)
            {
                if (h == null) continue;

                // distance from enemy center to the collider's closest point
                Vector2 closest = h.ClosestPoint(enemyPos);
                float dist = Vector2.Distance(enemyPos, closest);

                Dash();
            }
        }
        animDriver.Tick();
    }

    public void TakeDamage(float damage)
    {
        if (_dead) return;
        damageFlash.Flash();
        StartCoroutine(animDriver.RunAction(0.25f, Animator.StringToHash("Hurt")));
        _currentHealth -= damage;
        popup.CreatePopUp(damage, transform.position, 2);
        if (_currentHealth <= 0 && _dying == false) {
            StopAllCoroutines();
            _dying = true;
            if (onDeathEffect != null)
            {
                onDeathEffect();
                Die();
            }
            else
            {
                _dead = true;
                Die();
            }
        }
    }

    private void Die()
    {
        _dead = true;
        _dying = true;

        //StopAllCoroutines();
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            mb.StopAllCoroutines();

        GetComponent<Collider2D>().enabled = false;
        DisableChildren();

        var sm = GetComponent<StateMachine>();
        sm.enabled = false;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        if (animDriver != null) animDriver.TriggerDead();

        telemetryManager.EnemyKilled();
        Destroy(gameObject, 2f);
    }

    private void DisableChildren()
    {
        // Disable all colliders
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // stop attack/AI scripts
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb != this)
                mb.enabled = false;
        }
    }

    public void ApplyStun(float duration)
    {
        RemainingStunDuration = duration;

    }
    public void GetKnockedBack(Vector2 direction, float distance)
    {
        StartCoroutine(KnockbackRoutine(direction, distance));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float distance)
    {
        if (_data.enemyType == EnemyType.Guardian) yield break;
        _agent.enabled = false;
        dashAttacking = false;
        bool hitWall = false;
        float dashDuration = 0.25f;
        Vector2 start = transform.position;
        Vector2 dir = direction.normalized;
        Vector2 end = start + dir * distance;

        int wallMask = LayerMask.GetMask("Wall");

        RaycastHit2D hit = Physics2D.Raycast(start, dir, distance, wallMask);
        if (hit.collider != null)
        {
            end = hit.point - direction * 0.1f;
            hitWall = true;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector2.Lerp(start, end, t);
            yield return null;
        }

        if (hitWall) TakeDamage(10f);
        _agent.enabled = true;
    }

    public void Dash()
    {
        StartCoroutine(animDriver.RunAction(0.15f, Animator.StringToHash("Dash")));
        StartCoroutine(DashRoutine());
        _nextDashTime = Time.time + dashCooldown;
    }

    private IEnumerator DashRoutine()
    {
        _agent.isStopped = true;
        _agent.enabled = false;

        float dashDuration = 0.15f;
        float bestDistanceFromPlayer = 0f;

        Vector2 origin = transform.position;
        Vector2 awayFromPlayer = (origin - (Vector2)_player.transform.position).normalized;
        Vector2 bestEnd = origin;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask("Wall", "Spike");
        filter.useTriggers = true; // include trigger spikes if needed

        float skin = 0.15f;

        for (int i = 0; i < 360; i++)
        {
            Vector2 direction = Rotate(awayFromPlayer, i).normalized ;

            int count = enemyCollider.Cast(direction, filter, _hits, maxDashLenght);

            float allowedDistance = maxDashLenght;
            if (count > 0)
            {
                allowedDistance = Mathf.Max(0f, _hits[0].distance - skin);
            }

            Vector2 candidateEnd = origin + direction * allowedDistance;

            float distanceFromPlayer = Vector2.Distance((Vector2)_player.transform.position, candidateEnd);

            if (distanceFromPlayer > bestDistanceFromPlayer)
            {
                bestDistanceFromPlayer = distanceFromPlayer;
                bestEnd = candidateEnd;
            }
        }

        if (Vector2.Distance(origin, bestEnd) < 2f)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
            _nextDashTime = Time.time;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);
            transform.position = Vector2.Lerp(origin, bestEnd, t);
            yield return null;
        }

        transform.position = bestEnd;
        _agent.enabled = true;
        _agent.Warp(transform.position);
        _agent.isStopped = false;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }


    public void SetDeathEffect(Action deathEffect)
    {
        onDeathEffect = deathEffect;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = Application.isPlaying ? HomePosition : transform.position;

        Gizmos.DrawWireSphere(center, _data.wanderRadius);
    }


    public NavMeshAgent GetAgent() { return _agent; }
    public IAttack GetAttack() { return _attack; }
    public Player GetPlayer() { return _player; }
    public float GetChaseRange() { return _data.chaseRange; }
    public float GetAttackRange() { return _data.attackRange; }
}
