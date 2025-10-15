using System.Collections;
using UnityEngine;

public class MeleeAttack : IAttack
{
    protected override IEnumerator SpecialAttack() {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed / 2);
        Debug.Log(gameObject.ToString() + " Made a special melee attack");
        yield return new WaitForSeconds(_attackSpeed / 2); 
    }
    protected override IEnumerator BasicAttack() {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed / 2);
        Debug.Log(gameObject.ToString() + " Made a basic melee attack");
        yield return new WaitForSeconds(_attackSpeed / 2); 
    }
}
