using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] float damage;

    TelemetryManager telemetryManager;


    void Start()
    {
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
        
    }



    void OnTriggerEnter2D(Collider2D other)
    {


        if (other.TryGetComponent<Player>(out Player player))
        {
            player.TakeDamage(damage, gameObject);
            telemetryManager.DamageTrack(3, damage);
            //destroy spikes
            Destroy(gameObject);
        }
        else if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            if (enemy.dashAttacking) return;
            if (enemy._data.enemyType != EnemyType.Guardian)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
    
    
}