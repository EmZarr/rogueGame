using UnityEngine;

public class HealthLoot : MonoBehaviour
{
    [SerializeField] float healthAmount;
    [SerializeField] Animator _barrelAnimator;
    Player player;
    Collider2D _collider;
    TelemetryManager telemetryManager;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        _collider = GetComponent<Collider2D>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.Heal(healthAmount);

            // prevent re-triggering
            _collider.enabled = false;
            telemetryManager.LootPickedUp();
            telemetryManager.HealthBarrelTaken();
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
