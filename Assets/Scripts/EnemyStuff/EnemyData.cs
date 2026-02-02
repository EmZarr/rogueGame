using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Settings")]
    [SerializeField] public float chaseRange;
    [SerializeField] public float attackRange;

    [SerializeField] public float health;

    [SerializeField] public float attackSpeed;
    [SerializeField] public float attackDelay;
    [SerializeField] public float damage;
    [SerializeField] public EnemyType enemyType;

    [Header("Idle/Wander")]
    [SerializeField] public float wanderRadius = 5f;
    [SerializeField] public Vector2 wanderWaitRange = new(2f, 4f);
}

public enum EnemyType{
    Melee,
    Ranged,
    Bomber
}
