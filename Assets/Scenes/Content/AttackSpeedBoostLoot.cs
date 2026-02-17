using UnityEngine;

public class AttackSpeedBoostLoot : MonoBehaviour
{
   [SerializeField] float attackSpeedIncreasePercent;

    Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //player.IncreaseAttackSpeed(attackSpeedIncreasePercent);
            Destroy(gameObject);
        }
    }
    
}