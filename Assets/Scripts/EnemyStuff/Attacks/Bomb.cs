using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    private DamageZone damageZone;
    private float _speed;
    private float _fuseTime;
    private Vector2 _targetPosition;
    private bool _isMoving;
    private float _dmg;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Start()
    {
        damageZone = GetComponentInChildren<DamageZone>();
    }

    public void GetThrown(Vector2 targetPosition, float speed, float delay, float damage)
    {
        _targetPosition = targetPosition;
        _speed = speed;
        _fuseTime = delay;
        _dmg = damage;
        _isMoving = true;
    }

    public void Reflect(Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
        damageZone.changeTeam();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isMoving) return;
        // Move the bomb towards the target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);

        // Stop moving once the bomb reaches the target
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            _isMoving = false;

            damageZone.Activate(_dmg, 0.1f,_fuseTime,() => Destroy(gameObject));
        }
    }
}
