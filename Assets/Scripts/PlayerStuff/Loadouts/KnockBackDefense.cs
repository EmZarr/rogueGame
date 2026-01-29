using UnityEngine;
using UnityEngine.Tilemaps;

public class KnockBackDefense : MonoBehaviour
{
    [SerializeField] float _lifeTime = 0.2f;

    Vector2 _dir;

    Vector2 _mousePos;

    float _deathTime;

    GameObject Player;

    public void Init(Vector2 mousePos){
         _mousePos = mousePos;
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

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.GetKnockedBack(transform.forward, 7.0f);
        }
    }
}