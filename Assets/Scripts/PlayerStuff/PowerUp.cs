using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [SerializeField] EnemyType _boostType;
    [SerializeField] Animator _barrelAnimator;
    Player _player;
    Collider2D _collider;

    public enum EnemyType
    {
        DamageBoost,
        AttackSpeedBoost,
        MoveSpeedBoost
    }
    void Start()
    {
        _player = FindFirstObjectByType<Player>();
        _collider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            switch (_boostType)
            {
                case EnemyType.DamageBoost:
                    _player.IncreaseDamage(0.1f);
                    break;
                case EnemyType.AttackSpeedBoost:
                    _player.IncreaseAttackSpeed(0.1f);
                    break;
                case EnemyType.MoveSpeedBoost:
                    _player.IncreaseMovespeed(0.05f);
                    break;
            }

            // prevent re-triggering
            _collider.enabled = false;

            // play explosion animation
            _barrelAnimator.SetTrigger("Explode");
        }
    }

    // This will be called by the animation
    public void DestroyBarrel()
    {
        Destroy(gameObject);
    }
}
