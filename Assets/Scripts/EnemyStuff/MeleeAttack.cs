using System.Collections;
using UnityEngine;

public class MeleeAttack : IAttack
{
    [SerializeField] private meleeDamageZone _meleeDamageZone;
    protected override IEnumerator SpecialAttack() {
        // Windup, make attack, wind down
        _meleeDamageZone.ActivateSpecial();
        yield return new WaitForSeconds(_attackSpeed / 2);
        yield return _meleeDamageZone.DealDamage(_damage*2, true);
        _meleeDamageZone.Deactivate();
        yield return new WaitForSeconds(_attackSpeed / 2);
    }
    protected override IEnumerator BasicAttack() {
        // Windup, make attack, wind down


        _meleeDamageZone.Activate();
        yield return new WaitForSeconds(_attackSpeed / 2);
        yield return _meleeDamageZone.DealDamage(_damage, false);
        _meleeDamageZone.Deactivate();
        yield return new WaitForSeconds(_attackSpeed / 2); 
    }
}
