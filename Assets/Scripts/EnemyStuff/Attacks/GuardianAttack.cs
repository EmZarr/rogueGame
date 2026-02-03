using System.Collections;
using UnityEngine;

public class GuardianAttack : IAttack
{
    [SerializeField] private DamageZone damageZone;


    protected override IEnumerator BasicAttack()
    {
        yield return new WaitForSeconds(_attackDelay);
        TransformFacePlayer(damageZone.transform, 1.3f);
        damageZone.Activate(_damage, 0.1f, _attackDelay);
        damageZone.knockBack = true;
        yield return new WaitForSeconds(0.1f + _attackDelay);
        _nextReadyTime = Time.time + _attackSpeed;
    }

}