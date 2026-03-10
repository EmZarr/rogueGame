using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SwordAndShield : LoadoutBase
{
    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _heavySword;
    SwordHitbox _heavySwordhitbox;

    float _stunDuration = 2f;

    float _heavyStunDuration = 3f;

    public SwordAndShield(Player player) : base(player)
    {
        _heavyAttackDuration = 0.65f;

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
        _defCD = 4f;
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

    public override IEnumerator HeavyDash(Vector2 direction, Vector2 mousePos, Transform transform)
    {
        _player.SetStunning(true, _heavyStunDuration);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(direction, mousePos, transform);
        _player.SetStunning(false, _heavyStunDuration);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 direction)
    {
        _player.SetBlocking(_defenseDuration, direction);
        yield return new WaitForSeconds(_defenseDuration);
        RipostMeleeAttack(_player.GetMouseDir());
    }

    private void RipostMeleeAttack(Vector2 dir)
    {
        if (_heavySwordhitbox == null) return;

        Vector2 playerPos = _player.transform.position;

        // world-space placement
        Vector3 pos = playerPos + dir * 4f;
        pos.z = _heavySword.transform.position.z;
        _heavySword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _heavySword.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);

        float ripostDamage = _player.GetDamageBlocked() * 0.5f;
        if (ripostDamage >= 10f) _heavySwordhitbox.Activate(ripostDamage, GetHeavyAttackDuration() - GetHeavyAttackWindup());

    }
}


