using System.Collections;
using UnityEngine;

public class DualSwords : LoadoutBase
{

    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _aoeSword;
    SwordHitbox _aoeSwordhitbox;

    float _heavyDashDamage = 20f;

    float _parryTime = 0.4f;
    float _parryStunDuration = 3f;
    public DualSwords(Player player) : base(player)
    {
        _lightDamage = 5f;
        _attackSpeed = 0.25f;
        _lightDashCD = 0.5f;

        _sword = GameObject.FindGameObjectWithTag("Sword");
        if (_sword != null)
        {
            _swordhitbox = _sword.GetComponent<SwordHitbox>();
        }

        _aoeSword = GameObject.FindGameObjectWithTag("AOESword");
        if (_aoeSword != null)
        {
            _aoeSwordhitbox = _aoeSword.GetComponent<SwordHitbox>();
        }
    }

    public override IEnumerator LightAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _sword, _swordhitbox, 1.3f, _lightDamage);
    }

    public override IEnumerator HeavyAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _aoeSword, _aoeSwordhitbox, 0f, _heavyDamage);
    }

    public override IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
        _player.SetDealingDamage(true, _heavyDashDamage);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(transform, mousePos);
        _player.SetDealingDamage(false, _heavyDashDamage);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 mousePos)
    {
        _player.SetParrying(true, _parryTime, getMouseDir(mousePos), _parryStunDuration);
        yield return new WaitForSeconds(_parryTime);
        _player.SetParrying(false, _parryTime, getMouseDir(mousePos), _parryStunDuration);
    }
}
