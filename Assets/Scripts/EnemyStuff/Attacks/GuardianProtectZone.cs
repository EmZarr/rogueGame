using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GuardianProtectZone : DamageZone
{

    private GameObject _player;

    private bool _animating;

    [SerializeField] float maxHealth = 30f;
    [SerializeField] float healRate = 5f;
    [SerializeField] Enemy enemy;
    float _health;
    [SerializeField] DamageFlash _damageFlash;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player");
        _dmg = 5f;
        _health = maxHealth;
    }

    private void Update()
    {
        _hit = false;
        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
        }
        if (_health < maxHealth)
        {
            healShield(Time.deltaTime * healRate);
        }
        if (!enemy.IsStunned) {
            UpdateFacingTransform(1.3f, 40f);
        }
    }
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        var direction = (other.transform.position - transform.position).normalized;
        var player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(_dmg, transform.parent.gameObject);

            if (knockBackDistance > 0.0f && !player.IsInvinsible())
            {
                player.GetKnockedBack(direction, knockBackDistance);
            }

            if (!_animating)
            {
                Vector2 hitDir = (other.transform.position - transform.position).normalized;
                StartCoroutine(ShieldPush(hitDir));
            }
            _hit = true;
        }

        var projectile = other.GetComponent<Projectile>();
        if (projectile != null && projectile.IsAllied())
        {
            projectile.Reflect(direction);
        }
    }


    private IEnumerator ShieldPush(Vector2 direction)
    {
        _animating = true;
        var startPos = transform.localPosition;
        Vector3 pushOffset = (Vector3)direction * 0.6f;
        Vector3 targetPos = startPos + pushOffset;

        float t = 0f;

        // Push forward
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        t = 0f;

        // Pull back
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f;
            transform.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        transform.localPosition = startPos;
        _animating = false;
    }

    private void UpdateFacingTransform(
    float fixedDistance,
    float turnSpeedDegreesPerSecond)
    {
        Transform parent = transform.parent;

        if (!transform || !parent || !_player)
            return;

        Vector2 toPlayer = (Vector2)(_player.transform.position - parent.position);
        if (toPlayer.sqrMagnitude < 0.00001f)
            return;

        // Desired angle around Z
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        // Current angle based on current position around parent
        Vector2 fromParent = (Vector2)(transform.position - parent.position);
        float currentAngle = Mathf.Atan2(fromParent.y, fromParent.x) * Mathf.Rad2Deg;

        // Turn with speed limit
        float newAngle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            turnSpeedDegreesPerSecond * Time.deltaTime
        );

        // Maintain fixed distance from parent
        Vector2 offset = new Vector2(
            Mathf.Cos(newAngle * Mathf.Deg2Rad),
            Mathf.Sin(newAngle * Mathf.Deg2Rad)
        ) * fixedDistance;

        transform.position = (Vector2)parent.position + offset;
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle + 90f);
    }

    public void TakeDamage(float damage)
    {
        _damageFlash.Flash();
        _health -= damage;
        if ( _health <= 0)
        {
            sr.enabled = false;
            col.enabled = false;
            enemy.ApplyStun(4f);
        }
    }

    public void healShield(float heal)
    {
        _health += heal;
        if (_health >= maxHealth)
        {
            sr.enabled = true;
            col.enabled = true;
        }
    }

}