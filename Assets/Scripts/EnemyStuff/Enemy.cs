using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private IAttack _attack;
    private GameObject _player;
    private float _currentHealth;
    public float RemainingStunDuration { get; private set; }
    public bool IsStunned => RemainingStunDuration > 0f;

    public Vector3 HomePosition { get; private set; }
    public float WanderRadius => _data.wanderRadius;
    public Vector2 WanderWaitRange => _data.wanderWaitRange;

    private void Awake()
    {
        _player = GameObject.FindWithTag("Player");
        _currentHealth = _data.health;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        HomePosition = transform.position;
    }

    private void Update()
    {
        // For combat back
        /*if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
        }*/

        if (RemainingStunDuration > 0f)
        {
            RemainingStunDuration -= Time.deltaTime;
        }
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0) Destroy(gameObject);
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


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = Application.isPlaying ? HomePosition : transform.position;

        Gizmos.DrawWireSphere(center, _data.wanderRadius);
    }


    public NavMeshAgent GetAgent() { return _agent; }
    public IAttack GetAttack() { return _attack; }
    public GameObject GetPlayer() { return _player; }
    public float GetChaseRange() { return _data.chaseRange; }
    public float GetAttackRange() { return _data.attackRange; }
}
