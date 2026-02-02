using System.Collections;
using UnityEngine;

public class RangedAttack : IAttack
{
    [SerializeField] private Projectile _projectilePrefab;
    /*protected override IEnumerator SpecialAttack()
    {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed / 2);
        var proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        proj.SetInstigator(gameObject);
        proj.Init(true, _damage*2);
        yield return new WaitForSeconds(_attackSpeed / 2);
    }*/
    protected override IEnumerator BasicAttack()
    {
        yield return new WaitForSeconds(_attackDelay);
        var proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        proj.SetInstigator(gameObject);
        proj.Init(false, _damage);
        _nextReadyTime = Time.time + _attackSpeed;
    }
}
