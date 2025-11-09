using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] float healthAmount;

    Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.Heal(healthAmount);
            Destroy(gameObject);
        }
    }
    
}
