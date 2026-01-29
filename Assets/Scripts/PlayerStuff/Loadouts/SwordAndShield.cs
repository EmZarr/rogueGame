using System.Collections;
using UnityEngine;

public class SwordAndShield : LoadoutBase
{
    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _heavySword;
    SwordHitbox _heavySwordhitbox;

    float _shieldUpTime = 2f;

    float _stunDuration = 2f;

    float _heavyStunDuration = 5f;

    public SwordAndShield(Player player) : base(player)
    {
        _sword = GameObject.FindGameObjectWithTag("Sword");
        if (_sword != null)
        {
            _swordhitbox = _sword.GetComponent<SwordHitbox>();
        }

        _heavySword = GameObject.FindGameObjectWithTag("HeavySword");
        if (_heavySword != null)
        {
            _heavySwordhitbox = _heavySword.GetComponent<SwordHitbox>();
        }
    }

    public override IEnumerator LightAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _sword, _swordhitbox, 1.3f, _lightDamage);
    }

    public override IEnumerator HeavyAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _heavySword, _heavySwordhitbox, 4f, _heavyDamage);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
        _player.SetStunning(true, _stunDuration);
        yield return base.LightDash(direction, transform, mousePos);
        _player.SetStunning(false, _stunDuration);
    }

    public override IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
        _player.SetStunning(true, _heavyStunDuration);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(transform, mousePos);
        _player.SetStunning(false, _heavyStunDuration);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 mousePos)
    {
        _player.SetBlocking(_shieldUpTime, getMouseDir(mousePos));
        yield return new WaitForSeconds(_shieldUpTime);
    }
}


