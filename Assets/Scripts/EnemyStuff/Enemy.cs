using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private IAttack _attack;
    private GameObject _player;
    private float _currentHealth;

    private void Awake()
    {
        _player = GameObject.FindWithTag("Player");
        _currentHealth = _data.health;
    }


    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth < 0)
        {
            Destroy(gameObject);
        }
    }

    public NavMeshAgent GetAgent(){ return _agent; }
    public IAttack GetAttack() { return _attack;}
    public GameObject GetPlayer() { return _player; }
    public float GetChaseRange() { return _data.chaseRange; }
    public float GetAttackRange() { return _data.attackRange; }
}
