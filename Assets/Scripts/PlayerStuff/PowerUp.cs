using UnityEngine;

public class PowerUp : MonoBehaviour
{
    int _boostType;
    [SerializeField] Animator _barrelAnimator;
    Player _player;
    Collider2D _collider;

    TelemetryManager telemetryManager;

    void Start()
    {
        _player = FindFirstObjectByType<Player>();
        _collider = GetComponent<Collider2D>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
        _boostType = Random.Range(0, 3);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            switch (_boostType)
            {
                case 0:
                    _player.IncreaseDamage(0.1f);
                    break;
                case 1:
                    _player.IncreaseAttackSpeed(0.1f);
                    break;
                case 2:
                    _player.IncreaseMovespeed(0.05f);
                    break;
            }

            // prevent re-triggering
            _collider.enabled = false;
            telemetryManager.LootPickedUp();

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
