using UnityEngine;
public class SpeedBoostLoot : Loot
{
   [SerializeField] float speedIncreasePercent;
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.IncreaseMovespeed(speedIncreasePercent);
            base.telemetryManager.LootPickedUp();
            Destroy(gameObject);
        }
    }
}