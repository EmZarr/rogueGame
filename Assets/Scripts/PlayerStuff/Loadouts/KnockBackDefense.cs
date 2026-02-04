using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class KnockBackDefense : MonoBehaviour
{
    [SerializeField] float _lifeTime = 1000000f;

    Vector2 _dir;

    float _deathTime;

    bool active = false;

    GameObject Player;

    public void Init(Vector2 dir){
        Player = GameObject.FindGameObjectWithTag("Player");
         active = true;
         _dir = dir;
         // Rotate projectile to face direction of travel
         float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
         transform.rotation = Quaternion.Euler(0f, 0f, 90f + angle);
         transform.position = Player.transform.position + (Vector3)(_dir*2.0f);
         if (gameObject.GetComponent<BoxCollider2D>().enabled)
        {
            Debug.Log("Is enabled");
        }
         SetDeathTime();
    }

    void SetDeathTime()
    {
        _deathTime = Time.time + _lifeTime;
    }

    void Update()
    {
        if(active){
        if (Time.time >= _deathTime)
        {
            Destroy(gameObject);
            return;
        }
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            var direction = enemy.transform.position - Player.transform.position;
            enemy.GetKnockedBack(direction, 3.0f);
        }

        else if (other.TryGetComponent<TilemapCollider2D>(out TilemapCollider2D wall))
        {
            Debug.Log("wall");
        }
    }
}