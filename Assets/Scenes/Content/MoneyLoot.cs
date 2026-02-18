using UnityEngine;

public class MoneyLoot : Loot
{
   [SerializeField] int moneyAmount;

    //Player player;

    void Start()
    {
        //player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.IncreaseScore(moneyAmount);
            base.telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}

