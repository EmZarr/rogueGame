using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LoadoutBase
{

    [Header("Left Click")]
    protected float _windupProcent = 0.4f;
    protected float _lightAttackDuration = 0.5f;
    protected float _heavyAttackDuration = 1f;
    protected float _defenseDuration = 2f;
    protected float _lightDashDuration = 0.15f;
    protected float _HeavyDashDuration = 0.2f;
    protected float _lightDamage = 10f;
    protected float _heavyDamage = 20f;

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
        Debug.Log("Do defense");
        yield return new WaitForSeconds(0.1f);
        Debug.Log("Do defense");
    }

    public float getLightDashCD()
    {
        return _lightDashCD;
    }

    // Wtf is this?
    public float getHeavyDashCD()
    {
        return _heavyDashCD * _player.HeavyDashCooldownDecrease > 0 ? _heavyDashCD * _player.HeavyDashCooldownDecrease : 0;
    }

    public float GetHeavyDashCD2()
    {
        return _heavyDashCD;
    }

    public float getDefenseCD()
    {
        return _defCD;
    }

    public float getLightDamage()
    {
        return _lightDamage * _player.BaseDamageAmp + _lightDamage * _player.DamageAmp;
    }

    public float getHeavyDamage()
    {
        return _heavyDamage * _player.BaseDamageAmp + _heavyDamage * _player.DamageAmp;
    }

    /*public float getAttackSpeed()
    {
        return _lightAttackDuration * _player.AttackSpeedIncrease > 0 ? _lightAttackDuration * _player.AttackSpeedIncrease : 0;
    }*/


    //override in subclasses
    public float GetLightAttackDuration(){
        return _lightAttackDuration * _player.AttackSpeedIncrease > 0 ? _lightAttackDuration * _player.AttackSpeedIncrease : 0;
    }
    public float GetHeavyAttackDuration(){
        return _heavyAttackDuration * _player.AttackSpeedIncrease > 0 ? _heavyAttackDuration * _player.AttackSpeedIncrease : 0;
    }

    public float GetTempDamageBoost() => _player.DamageAmp;

    public float GetPermDamageBoost() => _player.BaseDamageAmp;

    public float GetLightAttackWindup() => GetLightAttackDuration() * _windupProcent;

    public float GetHeavyAttackWindup() => GetHeavyAttackDuration() * _windupProcent;
    public virtual float GetDefenseDuration() => _defenseDuration;
    public virtual float GetLightDashDuration() => _lightDashDuration;
    public virtual float GetHeavyDashDuration() => _HeavyDashDuration;

    public virtual float GetAttackCooldown(bool heavy)
    {
        return heavy ? GetHeavyAttackDuration()
                     : GetLightAttackDuration();
    }

    protected IEnumerator MeleeAttack(Vector2 dir, GameObject mySword, SwordHitbox mySwordHitbox, float distance, bool isHeavy, float angleOffset = -90f)
    {
        if (mySwordHitbox == null) yield break;

        Vector2 playerPos = _player.transform.position;

        // world-space placement
        Vector3 pos = playerPos + dir * distance;
        pos.z = mySword.transform.position.z;
        mySword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        mySword.transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);

        if (isHeavy) {
            yield return new WaitForSeconds(GetHeavyAttackWindup());
            mySwordHitbox.Activate(getHeavyDamage(), GetHeavyAttackDuration() - GetHeavyAttackWindup());
            yield return new WaitForSeconds(GetHeavyAttackDuration() - GetHeavyAttackWindup());
        }
        else
        {
            yield return new WaitForSeconds(GetLightAttackWindup());
            mySwordHitbox.Activate(getLightDamage(), GetLightAttackDuration() - GetLightAttackWindup());
            yield return new WaitForSeconds(GetLightAttackDuration() - GetLightAttackWindup());
        }
    }
}
