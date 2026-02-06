using UnityEngine;

public class SpeedBoostLoot : MonoBehaviour
{
   [SerializeField] float speedIncreasePercent;

    GameObject player;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

 
     void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.GetComponent<LoadoutState>().IncreaseMaxSpeed(speedIncreasePercent);
            Destroy(gameObject);
        }
    }
    
}