using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] int healthAmount;

    Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AddHealth();
        }
    }
    
    
    //Change  _health to be public or add public method in player, saving for later to avoid merge conflict
    void AddHealth()
    {
        //player._health += healthAmount;
    }
}
