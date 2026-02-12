using System.Collections;
using UnityEngine;

public class MeleeAttack : IAttack, ICancelableAttack
{
    [SerializeField] private DamageZone damageZone;

    /*protected override IEnumerator SpecialAttack() {
        // Windup, make attack, wind down
        _meleeDamageZone.ActivateSpecial();
        yield return new WaitForSeconds(_attackSpeed / 2);
        yield return _meleeDamageZone.DealDamage(_damage*2, true);
        _meleeDamageZone.Deactivate();
        yield return new WaitForSeconds(_attackSpeed / 2);
    }*/
    protected override IEnumerator BasicAttack() {
        TransformFacePlayer(damageZone.transform, 1.3f);
        damageZone.Activate(_damage, 0.1f, _attackDelay);
        yield return new WaitForSeconds(0.1f + _attackDelay);
        _nextReadyTime = Time.time + _attackSpeed;
    }

    public void CancelAttack()
    {
        damageZone.Cancel();
    }
}
