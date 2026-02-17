using System;
using System.Collections;
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

    private Collider2D bodyCol2D;        // assign your enemy body collider here

    [SerializeField] private EnemyAnimDriver animDriver;

    private Player _player;
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
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    bool _dead;

    private void OnEnable()
    {
        MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;

        // catch up in case player already spawned
        if (_player == null)
            HandlePlayerSpawned(MapInstantiator.CurrentPlayer);
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
        bodyCol2D = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // For combat back
        if (RemainingStunDuration > 0f)
        {
            RemainingStunDuration -= Time.deltaTime;
        }
        if (canDash && maxDashLenght > 0f && !IsStunned && !attacking && _player != null)
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
        if (_currentHealth <= 0 && _dying == false) {
            StopAllCoroutines();
            _dying = true;
            if (onDeathEffect != null)
            {
                onDeathEffect();
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

        StopAllCoroutines();

        var sm = GetComponent<StateMachine>();
        sm.enabled = false;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        if (animDriver != null) animDriver.TriggerDead();

        Destroy(gameObject, 2f);
    }

    public void ApplyStun(float duration)
    {
        RemainingStunDuration = duration;

    }
    public void GetKnockedBack(Vector2 direction, float distance)
    {
        StartCoroutine(KnockbackRoutine(direction, distance));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float distance)
    {
        _agent.enabled = false;
        float dashDuration = 0.25f;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * distance);

        NavMeshHit hit;
        if (NavMesh.Raycast(start, end, out hit, NavMesh.AllAreas))
        {
            end = hit.position;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null; // wait one frame
        }

        _agent.enabled = true;
        //transform.position = end; // snap cleanly at the end
   
    }

    public void Dash()
    {
        StartCoroutine(animDriver.RunAction(0.15f, Animator.StringToHash("Dash")));
        StartCoroutine(DashRoutine());
        _nextDashTime = Time.time + dashCooldown;
    }

    private IEnumerator DashRoutine()
    {
        // Safety
        if (_player == null || bodyCol2D == null)
            yield break;

        // Disable agent so it doesn't fight our manual movement
        bool agentWasEnabled = _agent != null && _agent.enabled;
        if (agentWasEnabled)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        float dashDuration = 0.15f;

        Vector2 origin = transform.position;

        // You were using "away from player" as base direction
        Vector2 baseDir = (origin - (Vector2)_player.transform.position);
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;
        baseDir.Normalize();

        // Build a ContactFilter once
        var filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Wall"));
        filter.useTriggers = false;

        float bestScore = float.NegativeInfinity;
        Vector2 bestEnd = origin;

        // Search angles; pick the candidate end that maximizes distance from player (your heuristic)
        for (int i = 0; i < 360; i++)
        {
            float angle = (360f * i) / 360;
            Vector2 dir = (Vector2)(Quaternion.Euler(0, 0, angle) * baseDir);
            dir.Normalize();

            float allowed = maxDashLenght;

            // Sweep the ENEMY COLLIDER along dir, not a ray
            int count = bodyCol2D.Cast(dir, filter, _hits, maxDashLenght);

            if (count > 0)
            {
                float nearest = float.PositiveInfinity;
                for (int h = 0; h < count; h++)
                    nearest = Mathf.Min(nearest, _hits[h].distance);

                // Pull back slightly so we don't end up in the wall
                allowed = Mathf.Max(0f, nearest - 0.05f);
            }

            Vector2 candidateEnd = origin + dir * allowed;

            float score = Vector2.Distance((Vector2)_player.transform.position, candidateEnd);
            if (score > bestScore)
            {
                bestScore = score;
                bestEnd = candidateEnd;
            }
        }

        // Optional: abort dash if too short (your original behavior)
        if (Vector2.Distance(origin, bestEnd) < 4f)
        {
            if (agentWasEnabled)
            {
                _agent.enabled = true;
                _agent.Warp(transform.position); // keep agent synced
                _agent.isStopped = false;
            }
            _nextDashTime = Time.time; // allow retry
            yield break;
        }

        // Move in fixed steps (don’t use Update-time delta here)
        float t = 0f;
        Vector2 start = origin;
        Vector2 end = bestEnd;

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / dashDuration;
            Vector2 pos = Vector2.Lerp(start, end, t);
            transform.position = pos;
            yield return new WaitForFixedUpdate();
        }

        transform.position = end;

        // Re-enable agent and sync it to our new position
        if (agentWasEnabled)
        {
            _agent.enabled = true;
            _agent.Warp(transform.position);
            _agent.isStopped = false;
        }
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
