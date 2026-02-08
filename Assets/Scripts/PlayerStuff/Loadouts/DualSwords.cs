using System.Collections;
using UnityEngine;

public class DualSwords : LoadoutBase
{

    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _aoeSword;
    SwordHitbox _aoeSwordhitbox;

    float _heavyDashDamage = 30f;

    float _parryStunDuration = 3f;


    public DualSwords(Player player) : base(player)
    {
        _lightDamage = 5f;
        _lightAttackDuration = 0.25f;
        _heavyAttackDuration = 0.5f;
        _lightDashCD = 0.5f;
        _defenseDuration = 0.4f;

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

    public override IEnumerator LightAttack(Vector2 direction)
    {
        yield return MeleeAttack(direction, _sword, _swordhitbox, 1.3f, false);
    }

    public override IEnumerator HeavyAttack(Vector2 direction)
    {
        yield return MeleeAttack(direction, _aoeSword, _aoeSwordhitbox, 0f, true);
    }

    public override IEnumerator HeavyDash(Vector2 direction, Transform transform)
    {
        _player.SetDealingDamage(true, _heavyDashDamage);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(direction, transform);
        _player.SetDealingDamage(false, _heavyDashDamage);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 direction)
    {
        _player.SetParrying(true, _defenseDuration, direction, _parryStunDuration);
        yield return new WaitForSeconds(_defenseDuration);
        _player.SetParrying(false, _defenseDuration, direction, _parryStunDuration);
    }
}
