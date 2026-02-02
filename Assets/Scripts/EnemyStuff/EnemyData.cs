using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Settings")]
    [SerializeField] public float chaseRange;
    [SerializeField] public float attackRange;
    [SerializeField] public float health;
    [SerializeField] public float attackSpeed;
    [SerializeField] public float specialCooldown;
    [SerializeField] public float damage;
    [SerializeField] public bool ranged;

    [Header("Idle/Wander")]
    [SerializeField] public float wanderRadius = 5f;
    [SerializeField] public Vector2 wanderWaitRange = new(2f, 4f);
}
