using UnityEngine;

public class MoneyLoot : MonoBehaviour
{
   [SerializeField] int moneyAmount;

    Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AddMoney();
        }
    }
    
    
    //Change  _health to be public or add public method in player, saving for later to avoid merge conflict
    void AddMoney()
    {
        //player._money += moneyAmount;
    }
}

