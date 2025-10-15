using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _speed = 10f;
    [SerializeField] float _damage = 5f;
    [SerializeField] float _lifeTime = 5f;

    Vector3 _dir;       // normalized direction to travel
    float _deathTime;

    // Call this right after Instantiate
    private void Awake()
    {
        var playerPos = GameObject.FindWithTag("Player").transform.position;
        _dir = (playerPos - transform.position).normalized;
    }

    void OnEnable()
    {
        _deathTime = Time.time + _lifeTime;
    }

    void Update()
    {
        // lifetime
        if (Time.time >= _deathTime) { Destroy(gameObject); return; }

        // move
        transform.position += _dir * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player)) {
            player.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
