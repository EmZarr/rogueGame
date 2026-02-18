using UnityEngine;

public class SpeedBoostLoot : Loot
{
   [SerializeField] float speedIncreasePercent;

    //GameObject player;

    void Start()
    {
        //player = GameObject.FindWithTag("Player");
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //player.GetComponent<LoadoutState>().IncreaseMaxSpeed(speedIncreasePercent);
            base.telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
    
}