
using UnityEngine;
using UnityEngine.Tilemaps;

public class TwoXArrowLogic : MonoBehaviour
{
    [SerializeField] float _speed = 10f;
    [SerializeField] float _lifeTime = 5f;

    Vector2 _dir;
    float _deathTime;
    float _damage;

    bool active = false;

    bool _reflected = false;

    public void Init(float damage, Vector2 dir, bool heavy, bool reflected)
    {
        _damage = damage;
        active = true;
        _reflected = reflected;
        SetDeathTime();
        _dir = dir;

        // Rotate projectile to face direction of travel
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, 90f + angle);
        if (heavy)
        {
            _speed = 30f;
        }
    }


    void SetDeathTime()
    {
        _deathTime = Time.time +_lifeTime;
    }

    void Update()
    {
        if (active){
        if (Time.time >= _deathTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(_damage);
            Destroy(gameObject);
        }
        else if (other.TryGetComponent<GuardianProtectZone>(out GuardianProtectZone guardianProtectZone))
        {
            guardianProtectZone.TakeDamage(_damage);
        }
        else if (_reflected && other.TryGetComponent<Player>(out Player player))
        {
            player.TakeDamage(_damage, gameObject);
            Destroy(gameObject);
        }
        else if (other.TryGetComponent<TilemapCollider2D>(out TilemapCollider2D wall))
        {
            Destroy(gameObject);
        }
    }
}
