using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LoadoutBase
{

    [Header("Left Click")]
    protected float _lightWindup = 0.1f;
    protected float _lightDamage = 10f;
    protected float _heavyDamage = 20f;
    protected float _attackSpeed = 0.5f;

    [Header("Right Click")]
    protected float _defCD = 2f;


    [Header("Space")]
    protected float _heavyDashCD = 5f;
    protected float _lightDashCD = 1f;

    protected Player _player;

    public LoadoutBase(Player player)
    {
        _player = player;
    }

    public virtual IEnumerator LightAttack(Vector2 mousePos)
    {
        yield return new WaitForSeconds(_lightWindup);

        Debug.Log("Do light attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator HeavyAttack(Vector2 mousePos) {
        Debug.Log("Do heavy attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
        float dashDistance = 4f;
        float dashDuration = 0.15f;

        direction.Normalize();

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * dashDistance);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
    public virtual IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
        float dashDistance = 12f;
        float dashDuration = 0.2f;

        var direction = getMouseDir(mousePos);
        direction.Normalize();

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * dashDistance);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }


    public virtual IEnumerator Defense(Vector2 mousePos)
    {
        Debug.Log("Do defense");
        yield return new WaitForSeconds(0.1f);
    }

    public float getLightDashCD()
    {
        return _lightDashCD;
    }

    public float getHeavyDashCD()
    {
        return _heavyDashCD;
    }

    public float getDefenseCD()
    {
        return _defCD;
    }

    protected Vector2 getMouseDir(Vector2 mousePos)
    {
        Transform player = _player.transform;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;
        return dir;
    }

    protected IEnumerator MeleeAttack(Vector2 mousePos, GameObject mySword, SwordHitbox mySwordHitbox, float distance, float damage)
    {
        if (mySwordHitbox == null) yield break;

        Vector2 playerPos = _player.transform.position;

        Vector2 dir = getMouseDir(mousePos);

        // world-space placement
        Vector3 pos = playerPos + dir * distance;
        pos.z = mySword.transform.position.z;
        mySword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        mySword.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);

        // short active window
        mySwordHitbox.Activate(damage, 0.1f);

        yield return new WaitForSeconds(_attackSpeed);
    }
}
