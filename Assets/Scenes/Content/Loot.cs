using UnityEngine;

public class Loot : MonoBehaviour
{
    public Player player;
    public TelemetryManager telemetryManager;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
    }

 
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //player.IncreaseAttackSpeed(attackSpeedIncreasePercent);
            telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}