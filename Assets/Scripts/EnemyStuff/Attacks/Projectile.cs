using UnityEngine;
using UnityEngine.Tilemaps;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _speed = 10f;

    Vector2 _dir;
    float _damage;
    bool _allied;

    GameObject _instigator;

    public void SetInstigator(GameObject instigator) => _instigator = instigator;
    public void Init(float damage, Vector2 dir, bool allied)
    {
        _damage = damage;
        _dir = dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector2.right;
        _allied = allied;
    }

    public void Reflect(Vector2 newDir)
    {
        _allied = !_allied;
        _dir = newDir.sqrMagnitude > 0.000001f ? newDir.normalized : Vector2.right;
        _speed = 1.5f * _speed;
    }

    public bool IsAllied()
    {
        return _allied; 
    }


    void Update()
    {
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, 90f + angle);
        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_allied && other.TryGetComponent<Player>(out Player player))
        {
            var killer = _instigator != null ? _instigator : gameObject;
            player.TakeDamage(_damage, killer);
            Destroy(gameObject);
        }
        if (_allied && other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(_damage);
            Destroy(gameObject);
        }
        else if (_allied && other.TryGetComponent<GuardianProtectZone>(out GuardianProtectZone guardianProtectZone))
        {
            guardianProtectZone.TakeDamage(_damage);
        }
        else if (other.tag == "Wall")
        {
            Destroy(gameObject);
        }
    }
}
