using System.Collections;
using UnityEngine;

public class RangedAttack : IAttack
{
    [SerializeField] private Projectile _projectilePrefab;
    protected override IEnumerator SpecialAttack()
    {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed / 2);
        var proj = Instantiate(_projectilePrefab, gameObject.transform);
        proj.transform.localScale = new Vector3(0.6f, 0.6f, 3f);
        yield return new WaitForSeconds(_attackSpeed / 2);
    }
    protected override IEnumerator BasicAttack()
    {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed/2);
        Instantiate(_projectilePrefab, gameObject.transform);
        yield return new WaitForSeconds(_attackSpeed / 2);
    }
}
