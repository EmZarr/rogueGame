using UnityEngine;

public class DamageBoostLoot : Loot
{
   [SerializeField] float damageIncreasePercent;
   [SerializeField] bool permanent;

    //Player player;

    void Start()
    {
        //player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //player.IncreaseDamage(damageIncreasePercent, permanent);
            base.telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}

