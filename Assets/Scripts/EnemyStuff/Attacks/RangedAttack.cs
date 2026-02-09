using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        Vector3 dir = (_player.transform.position - transform.position).normalized;
        Vector3 spawnPos = transform.position + dir * 0.5f;
        var proj = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
        proj.SetInstigator(gameObject);
        proj.Init(false, _damage, false);
        _nextReadyTime = Time.time + _attackSpeed;
    }

}
