using UnityEngine;

public class HeavyDashCooldownLoot : Loot
{
   [SerializeField] float cooldownDecreasePercent;

    //Player player;
    
    void Start()
    {
        //player = FindFirstObjectByType<Player>();
    }

 
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            base.telemetryManager.LootPickedUp();
            base.player.DecreaseHeavyDashCooldown(cooldownDecreasePercent);
            Destroy(gameObject);
        }
    }
    
}