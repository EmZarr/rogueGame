using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] float damage;

    Player player;
    TelemetryManager telemetryManager;

    Collider2D spikeCollider;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
        
        spikeCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        Collider2D[] hits = new Collider2D[10];

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;

        int count = spikeCollider.Overlap(filter, hits);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = hits[i];

            if (other.CompareTag("Enemy"))
            {
                    Enemy enemy = other.GetComponent<Enemy>();
                    enemy.TakeDamage(damage);
                    Destroy(gameObject);
            }
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {


        if (other.CompareTag("Player"))
        {
            player.TakeDamage(damage, gameObject);
            telemetryManager.DamageTrack(3, damage);
            //destroy spikes
            Destroy(gameObject);
        }
        //enemy
        /*if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemy entered spikes");
            Enemy enemy = other.GetComponent<Enemy>();
            //Debug.Log("Enemy entered spikes");
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                telemetryManager.DamageTrack(4, damage);

                Destroy(gameObject);
            }
        }*/
    }
    
    
}