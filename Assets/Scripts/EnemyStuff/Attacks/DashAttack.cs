using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DashAttack : IAttack, ICancelableAttack
{
    [SerializeField] private DamageZone damageZone;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float wallSkin = 0.05f;

    /*protected override IEnumerator SpecialAttack() {
        // Windup, make attack, wind down
        _meleeDamageZone.ActivateSpecial();
        yield return new WaitForSeconds(_attackSpeed / 2);
        yield return _meleeDamageZone.DealDamage(_damage*2, true);
        _meleeDamageZone.Deactivate();
        yield return new WaitForSeconds(_attackSpeed / 2);
    }*/
    protected override IEnumerator BasicAttack()
    {
        damageZone.Activate(_damage, 0.2f, _attackDelay);

        float trackTime = _attackDelay * 0.5f;
        float elapsed = 0f;

        while (elapsed < trackTime)
        {
            Vector2 start = transform.position;
            Vector2 dir = -damageZone.transform.up;
            Vector2 end = ComputeDashEnd(start, dir, 20f);

            // actual dash length (wall-clamped)
            float dashLen = Vector2.Distance(start, end);

            // Update scale and transform to match length
            Vector3 scale = damageZone.transform.localScale;
            scale.y = dashLen;
            damageZone.transform.localScale = scale;
            TransformFacePlayer(damageZone.transform, dashLen/2);


            elapsed += Time.deltaTime;
            yield return null;
        }

        // Finish windup without tracking
        yield return new WaitForSeconds(_attackDelay - trackTime);

        yield return StartCoroutine(DashThrough(-damageZone.transform.up ,0.2f, 20f));

        // Opening
        GetComponentInParent<Enemy>().ApplyStun(2f);
        _nextReadyTime = Time.time + _attackSpeed;
    }


    IEnumerator DashThrough(Vector2 direction, float dashDuration, float dashDistance)
    {
        Vector2 start = transform.position;
        Vector2 end = ComputeDashEnd(start, direction, dashDistance);

        float initialWidthX = damageZone.transform.localScale.x; // keep width constant

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            if (t > 1f) t = 1f;

            // Move player
            Vector2 pos = Vector2.Lerp(start, end, t);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            // Shrink zone behind player
            Vector2 back = pos;   // moving with player
            Vector2 front = end;  // fixed at dash destination

            Vector2 toFront = front - back;
            float remainingLen = toFront.magnitude;

            if (remainingLen <= 0.01f)
            {
                // practically gone
                damageZone.transform.localScale = new Vector3(initialWidthX, 0f, damageZone.transform.localScale.z);
            }
            else
            {
                // Here, we make damageZone.up point FROM center toward back->front direction.
                Vector2 dir = toFront / remainingLen;

                // Center in between
                Vector2 center = (back + front) * 0.5f;
                damageZone.transform.position = new Vector3(center.x, center.y, damageZone.transform.position.z);

                // Set rotation so local UP matches dir 
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                damageZone.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                // Update length
                damageZone.transform.localScale = new Vector3(initialWidthX, remainingLen, damageZone.transform.localScale.z);
            }

            yield return null;
        }

        // snap final position
        transform.position = new Vector3(end.x, end.y, transform.position.z);

    }


    Vector2 ComputeDashEnd(Vector2 start, Vector2 dir, float maxDistance)
    {
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxDistance, wallMask);

        if (hit.collider != null)
        {
            return hit.point - dir * wallSkin;
        }

        return start + dir * maxDistance;
    }

    public void CancelAttack()
    {
        damageZone.Cancel();
    }
}
