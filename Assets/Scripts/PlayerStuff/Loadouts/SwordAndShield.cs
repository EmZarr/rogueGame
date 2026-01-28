using System.Collections;
using UnityEngine;

public class SwordAndShield : LoadoutBase
{
    GameObject _shield;
    SpriteRenderer _shieldRenderer;
    Collider2D _shieldCollider;

    GameObject _sword;
    SwordHitbox _swordhitbox;

    float _shieldUpTime = 2f;

    public SwordAndShield()
    {
        _shield = GameObject.FindGameObjectWithTag("Shield");

        if (_shield != null)
        {
            _shieldRenderer = _shield.GetComponent<SpriteRenderer>();
            _shieldCollider = _shield.GetComponent<Collider2D>();
        }

        _sword = GameObject.FindGameObjectWithTag("Sword");
        if (_sword != null)
        {
            _swordhitbox = _sword.GetComponent<SwordHitbox>();
        }
    }

    public override IEnumerator LightAttack(Vector2 mousePos)
    {
        if (_swordhitbox == null) yield break;

        Transform player = _swordhitbox.transform.parent;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;

        float distance = 1.3f;

        // world-space placement
        Vector3 pos = playerPos + dir * distance;
        pos.z = _sword.transform.position.z;
        _sword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _sword.transform.rotation = Quaternion.Euler(0f, 0f, angle+90f);

        // short active window
        _swordhitbox.Activate(_lightDamage, 0.1f);

        yield return new WaitForSeconds(_attackSpeed);
    }
    public override IEnumerator Defense(Vector2 mousePos)
    {
        if (_shield == null) yield break;

        if (_shieldRenderer) _shieldRenderer.enabled = true;
        if (_shieldCollider) _shieldCollider.enabled = true;

        Transform player = _shield.transform.parent;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;

        float shieldDistance = 1.5f;

        // world-space placement
        Vector3 pos = playerPos + dir * shieldDistance;
        pos.z = _shield.transform.position.z;
        _shield.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _shield.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        yield return new WaitForSeconds(_shieldUpTime);
        if (_shieldRenderer) _shieldRenderer.enabled = false;
        if (_shieldCollider) _shieldCollider.enabled = false;
    }
}


