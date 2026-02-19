using System.Collections;
using UnityEngine;

public class SwordAndShield : LoadoutBase
{
    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _heavySword;
    SwordHitbox _heavySwordhitbox;

    float _stunDuration = 3f;

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
        _defCD = 0f;
    }

    public override IEnumerator LightAttack(Vector2 direction)
    {
        yield return MeleeAttack(direction, _sword, _swordhitbox, 1.3f, false);
    }

    public override IEnumerator HeavyAttack(Vector2 direction)
    {
        yield return MeleeAttack(direction, _heavySword, _heavySwordhitbox, 4f, true, 90f);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
        _player.SetStunning(true, _stunDuration);
        yield return base.LightDash(direction, transform, mousePos);
        _player.SetStunning(false, _stunDuration);
    }

    public override IEnumerator HeavyDash(Vector2 direction, Transform transform)
    {
        _player.SetStunning(true, _heavyStunDuration);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(direction, transform);
        _player.SetStunning(false, _heavyStunDuration);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 direction)
    {
        _player.SetBlocking(_defenseDuration, direction);
        yield return new WaitForSeconds(_defenseDuration);
    }
}


