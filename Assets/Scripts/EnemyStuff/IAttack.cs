using System.Collections;
using UnityEngine;

public abstract class IAttack : MonoBehaviour
{
    [SerializeField] EnemyData _data;
    protected float _cooldown;
    protected float _nextReadyTime = 0f;
    protected float _attackSpeed;


    private void Start()
    {
        _cooldown = _data.specialCooldown;
        _attackSpeed = _data.attackSpeed;
    }
    public IEnumerator Attack()
    {
        if (IsReady())
        {
            yield return SpecialAttack();   // must yield until the special is done
            _nextReadyTime = Time.time + _cooldown;
        }
        else
        {
            yield return BasicAttack();     // must yield until the basic is done
        }
    }
    public bool IsReady() { return Time.time >= _nextReadyTime; }
    protected abstract IEnumerator SpecialAttack();
    protected abstract IEnumerator BasicAttack();
}
