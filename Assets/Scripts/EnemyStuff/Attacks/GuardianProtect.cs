using System.Collections;
using UnityEngine;

public class GuardianProtect : IProtect
{
    [SerializeField] private GuardianProtectZone guardianProtectZone;

    protected override IEnumerator BasicProtect()
    {
        //yield return new WaitForSeconds(0.1f);
        TransformFacePlayer(guardianProtectZone.transform, 1.3f);
        guardianProtectZone.Activate(1.0f, 0.0f);
        //yield return new WaitForSeconds(0.1f);
      
        //_nextReadyTime = Time.time + _protectCooldown;
        _nextReadyTime = Time.time;
        yield return null;

    }
}