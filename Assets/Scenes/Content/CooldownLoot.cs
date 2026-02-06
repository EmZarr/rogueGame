using UnityEngine;

public class HeavyDashCooldownLoot : MonoBehaviour
{
   [SerializeField] float cooldownDecreasePercent;

    Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.DecreaseHeavyDashCooldown(cooldownDecreasePercent);
            Destroy(gameObject);
        }
    }
    
}