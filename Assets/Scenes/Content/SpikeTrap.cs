using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] float damage;

    Player player;
    TelemetryManager telemetryManager;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.TakeDamage(damage, gameObject);
            telemetryManager.DamageTrack(3, damage);
        }
    }
    
    
}