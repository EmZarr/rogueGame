using UnityEngine;

public class AttackSpeedBoostLoot : Loot
{
   [SerializeField] float attackSpeedIncreasePercent;

    //Player player;
    //TelemetryManager telemetryManager;

    void Start()
    {
        //player = FindFirstObjectByType<Player>();
        //telemetryManager = FindFirstObjectByType<TelemetryManager>();
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