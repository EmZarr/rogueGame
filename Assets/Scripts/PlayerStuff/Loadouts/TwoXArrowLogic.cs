
using UnityEngine;
using UnityEngine.Tilemaps;

public class TwoXArrowLogic : MonoBehaviour
{
    [SerializeField] float _speed = 10f;
    [SerializeField] float _lifeTime = 5f;

    Vector2 _dir;
    float _deathTime;
    float _damage;

    Vector2 _mousePos;

    public void Init(float damage, Vector2 mousePos)
    {
        _damage = damage;
        _mousePos = mousePos;
    }


    private void Awake()
    {
        _dir = (_mousePos - (Vector2)transform.position).normalized;

        // Rotate projectile to face direction of travel
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, 90f + angle);
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
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(_damage);
            Destroy(gameObject);
        }
        else if (other.TryGetComponent<TilemapCollider2D>(out TilemapCollider2D wall))
        {
            Destroy(gameObject);
        }
    }
}
