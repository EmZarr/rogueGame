using System;
using System.Collections;
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
        telemetryManager = FindAnyObjectByType<TelemetryManager>();
    }

    private void Update()
    {
        // For combat back
        if (RemainingStunDuration > 0f)
        {
            RemainingStunDuration -= Time.deltaTime;
        }
        if (canDash && maxDashLenght > 0f && !IsStunned && !attacking)
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

        telemetryManager.EnemyKilled();
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
        _agent.enabled = false;
        float dashDistance = 0f;
        float dashDuration = 0.15f;

        var direction = (Vector2)(transform.position -_player.transform.position);
        var baseDirection = direction.normalized;
        var finalEnd = Vector3.zero;
        
        direction.Normalize();
        Vector2 start = (Vector2) transform.position;

        start = start + direction*1.5f;
        Vector2 end = start + (direction * maxDashLenght);
        
        for (int i=0; i<360; i++)
        {
            direction = ((Vector2) (Quaternion.Euler(0, 0, i) * baseDirection)).normalized;
            start = (Vector2) transform.position - baseDirection*1.5f;
            end = (Vector2) transform.position + (direction * maxDashLenght);
            int mask = LayerMask.GetMask("Wall");

            RaycastHit2D hit = Physics2D.Raycast(
                start,
                direction,
                maxDashLenght,
                mask
            );

            if (hit.collider != null)
            {
                end = hit.point;
            } 
            var distance = Vector2.Distance((Vector2)_player.transform.position, end);
            if (distance > dashDistance)
            {
                dashDistance = distance;
                finalEnd = (Vector3)end;
            }
        }
        start = transform.position;
        end = finalEnd;
        
        if (Vector2.Distance((Vector2)transform.position, end) < 4.0f)
        {
            _agent.enabled = true;
            _nextDashTime = Time.time;
        }
        else {

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        transform.position = end;
        _agent.enabled = true;
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
