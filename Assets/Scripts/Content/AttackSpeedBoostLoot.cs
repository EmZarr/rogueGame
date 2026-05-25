using UnityEngine;
public class AttackSpeedBoostLoot : Loot
{
   [SerializeField] float attackSpeedIncreasePercent;
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.IncreaseAttackSpeed(attackSpeedIncreasePercent);
            telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}