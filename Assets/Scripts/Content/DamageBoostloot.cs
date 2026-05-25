using UnityEngine;

public class DamageBoostLoot : Loot
{
   [SerializeField] float damageIncreasePercent;
   [SerializeField] bool permanent;
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.IncreaseDamage(damageIncreasePercent);
            base.telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}

