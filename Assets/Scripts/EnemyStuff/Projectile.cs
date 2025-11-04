using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _speed = 10f;
    [SerializeField] float _lifeTime = 5f;

    Vector2 _dir;
    float _deathTime;
    float _damage;
    bool _unblockable = false;

    public void Init(bool unblockable, float damage)
    {
        _damage = damage;
        if (unblockable)
        {
            // Double the x and y scale
            transform.localScale = new Vector3(
                transform.localScale.x * 2f,
                transform.localScale.y * 2f,
                transform.localScale.z
            );

            _unblockable = true;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
            }
        }
    }

    public bool GetUnblockable()
    {
        return _unblockable;
    }

    private void Awake()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector2 playerPos = player.transform.position;
            _dir = (playerPos - (Vector2)transform.position).normalized;

            // Rotate projectile to face direction of travel
            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, 90f + angle);
        }
        else
        {
            Debug.LogWarning("No Player found with tag 'Player'. Projectile will not move.");
            _dir = Vector2.zero;
        }
    }

    void OnEnable()
    {
        _deathTime = Time.time + _lifeTime;
    }

    void Update()
    {
        if (Time.time >= _deathTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            player.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
