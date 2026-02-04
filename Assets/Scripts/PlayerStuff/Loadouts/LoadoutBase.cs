using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LoadoutBase
{

    [Header("Left Click")]
    protected float _windupProcent = 0.4f;
    protected float _lightAttackDuration = 0.3f;
    protected float _heavyAttackDuration = 1f;
    protected float _defenseDuration = 2f;
    protected float _lightDashDuration = 0.15f;
    protected float _HeavyDashDuration = 0.2f;
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

    public virtual IEnumerator LightAttack(Vector2 dir)
    {

        yield return new WaitForSeconds(GetLightAttackDuration());
        Debug.Log("Do light attack");
    }

    public virtual IEnumerator HeavyAttack(Vector2 dir) {
        yield return new WaitForSeconds(GetHeavyAttackDuration());
        Debug.Log("Do heavy attack");
    }

    public virtual IEnumerator LightDash(Vector2 direction, Transform transform)
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
    public virtual IEnumerator HeavyDash(Vector2 direction, Transform transform)
    {
        float dashDistance = 12f;
        float dashDuration = 0.2f;

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
        yield return new WaitForSeconds(0.1f);
        Debug.Log("Do defense");
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

    //override in subclasses
    public float GetLightAttackDuration() => _lightAttackDuration;
    public float GetHeavyAttackDuration() => _heavyAttackDuration;

    public float GetLightAttackWindup() => _lightAttackDuration * _windupProcent;

    public float GetHeavyAttackWindup() => _heavyAttackDuration * _windupProcent;
    public virtual float GetDefenseDuration() => _defenseDuration;
    public virtual float GetLightDashDuration() => _lightDashDuration;
    public virtual float GetHeavyDashDuration() => _HeavyDashDuration;

    public virtual float GetAttackCooldown(bool heavy)
    {
        return heavy ? _heavyAttackDuration * _attackSpeed
                     : _lightAttackDuration * _attackSpeed;
    }

    protected IEnumerator MeleeAttack(Vector2 dir, GameObject mySword, SwordHitbox mySwordHitbox, float distance, bool isHeavy)
    {
        if (mySwordHitbox == null) yield break;

        Vector2 playerPos = _player.transform.position;

        // world-space placement
        Vector3 pos = playerPos + dir * distance;
        pos.z = mySword.transform.position.z;
        mySword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        mySword.transform.rotation = Quaternion.Euler(0f, 0f, angle-90f);

        if (isHeavy) {
            yield return new WaitForSeconds(GetHeavyAttackWindup());
            mySwordHitbox.Activate(_heavyDamage, _heavyAttackDuration - GetHeavyAttackWindup());
            yield return new WaitForSeconds(_heavyAttackDuration - GetHeavyAttackWindup());
        }
        else
        {
            yield return new WaitForSeconds(GetLightAttackWindup());
            mySwordHitbox.Activate(_lightDamage, _lightAttackDuration - GetLightAttackWindup());
            yield return new WaitForSeconds(_lightAttackDuration - GetLightAttackWindup());
        }
    }
}
