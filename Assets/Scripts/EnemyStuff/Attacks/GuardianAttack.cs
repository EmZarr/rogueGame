using System.Collections;
using UnityEngine;

public class GuardianAttack : IAttack
{
    [SerializeField] private GuardianProtectZone guardianProtectZone;

    protected override IEnumerator BasicAttack()
    {
        TransformFacePlayer(guardianProtectZone.transform, 1.3f, 90f, 10f);
        yield return null;

    }
}