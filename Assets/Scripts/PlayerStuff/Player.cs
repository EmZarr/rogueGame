using System;

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    [SerializeField] float _maxSpeed = 8f;
    [SerializeField] private LayerMask _shieldMask;
    [SerializeField] DamageFlash _flash;
    [SerializeField] Animator _healthAnimator;
    [SerializeField] Animator _powerUpAnimator;
    [SerializeField] LoadoutState _loadoutState;
    [SerializeField] private Rigidbody2D rb;

    Shield _shield;
    UI _ui;
    float _score = 0;
    private Coroutine _tempBuffCo;
    public event Action<GameObject /*killer*/> OnDied;
    public static Player Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    private void OnDisable()
    {
        // optional: ensure no �stuck buff� if object is disabled mid-buff
        _tempBuffCo = null;
    }
    private bool _isInvinsible = false;
    private bool _isStunning = false;
    private float _stunDuration = 1f;
    private bool _isDealingDmg = false;
    private float _damage = 5f;

    private float _damageMultiplier = 1.0f;
    private float _attackSpeedMultiplier = 1.0f;
    private float _moveSpeedMultiplier = 1.0f;
    private float _activeTempMultiplier = 1f;

    public float HeavyDashCooldownDecrease = 1f;

    private float _parryStunDuration = 10f;


    bool _isRespawning;

    private void Start()
    {
        _ui = FindAnyObjectByType<UI>();
        _shield = GetComponentInChildren<Shield>();
    }
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (_isInvinsible) return;

        bool shieldIntercepts = attacker != null && AttackLineHitsShield(attacker.transform);

        if (_shield.isParrying && shieldIntercepts)
        {
            var enemy = attacker.GetComponentInParent<Enemy>();
            if (enemy != null && enemy._data.enemyType != EnemyType.Ranged)
            {
                enemy.ApplyStun(_parryStunDuration);
            }
            return;
        }

        if (_shield.isBlocking && shieldIntercepts)
        {
            return;
        }
        _flash.Flash();
        _health -= damage;
        if (_health <= 0)
        {
            OnDied?.Invoke(attacker);
            //Destroy(gameObject);
            StartCoroutine(RespawnRoutine());
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        _ui.updateScore(_score);
    }

    public void Heal(float health)
    {
        _health += health;
        _healthAnimator.SetTrigger("PickUpHealth");
        _ui.updateHealth(_health);
    }

    public void TeleportTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
    IEnumerator RespawnRoutine() {
        _isRespawning = true;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        yield return new WaitForSeconds(0.2f);
        _health = 100;
        _ui.updateHealth(_health);

        if (col) col.enabled = true;
        _isRespawning = false;
    }

    public void SetStunning(bool isStunning, float stunDuration)
    {
        _isStunning = isStunning;
        _stunDuration = stunDuration;
    }

    public void SetDealingDamage(bool isDealingdmg, float damageAmount)
    {
        _isDealingDmg = isDealingdmg;
        _damage = damageAmount*_damageMultiplier;
    }

    public void SetInvinsible(bool isInvinsible)
    {
        _isInvinsible = isInvinsible;
    }

    public void SetParrying(bool isParrying, float duration, Vector2 dir, float parryStunDuration)
    {
        if (isParrying)
        {
            _shield.Activate(duration, dir);
        }
        _shield.isParrying = isParrying;
        _parryStunDuration = parryStunDuration;
    }

    public void SetBlocking(float duration, Vector2 dir)
    {
        _shield.Activate(duration, dir);
    }

    public void OnHitboxTrigger(Collider2D other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (_isStunning)
            {
                enemy.ApplyStun(_stunDuration);
            }
            if (_isDealingDmg)
            {
                enemy.TakeDamage(_damage);
            }
        }
    }

    private bool AttackLineHitsShield(Transform attacker)
    {
        if (_shield == null) return false;

        var shieldCol = _shield.GetComponent<Collider2D>();
        if (shieldCol == null) return false;

        Vector2 from = attacker.position;
        Vector2 to = transform.position;

        RaycastHit2D hit = Physics2D.Linecast(from, to, _shieldMask);

        return hit.collider == shieldCol;
    }

    public void GetKnockedBack(Vector2 direction, float distance)
    {
        StartCoroutine(KnockbackRoutine(direction, distance));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir, float distance)
    {
        _loadoutState.SetMovementBlocked(true);
        dir = dir.normalized;

        float duration = 0.25f;

        // speed needed to travel distance in duration (ignoring collisions)
        float speed = distance / duration;

        // Take control of velocity during knockback
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.fixedDeltaTime;

            // override velocity so damping/mass doesn't make it "barely move"
            rb.linearVelocity = dir * speed;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        _loadoutState.SetMovementBlocked(false);
    }


    public void IncreaseMovespeed(float increase) {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _moveSpeedMultiplier += increase;
        _loadoutState.SetSpeed(1f);
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }
    public void IncreaseAttackSpeed(float increase)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _attackSpeedMultiplier += increase;
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }
    public void IncreaseDamage(float increase)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _damageMultiplier += increase;
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }

    public void TempDamageBoost(float multiplier = 2f, float duration = 10f)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        // remove existing temp boost first
        if (_tempBuffCo != null)
        {
            StopCoroutine(_tempBuffCo);
            _damageMultiplier /= _activeTempMultiplier;   // undo previous temp
            _activeTempMultiplier = 1f;
        }

        _tempBuffCo = StartCoroutine(TempBoostRoutine(multiplier, duration));
    }

    private IEnumerator TempBoostRoutine(float multiplier, float duration)
    {
        _activeTempMultiplier = multiplier;
        _damageMultiplier *= _activeTempMultiplier;

        yield return new WaitForSeconds(duration);

        _damageMultiplier /= _activeTempMultiplier;
        _activeTempMultiplier = 1f;
        _tempBuffCo = null;
    }

    public void DecreaseHeavyDashCooldown(float percentDecrease)
    {
        HeavyDashCooldownDecrease -= 0.01f*percentDecrease;
    }

    public bool IsInvinsible()
    {
        return _isInvinsible;
    }

    public float DamageMultiplier => _damageMultiplier;
    public float AttackSpeedMultiplier => _attackSpeedMultiplier;
    public float GetMoveSpeed() {
        return _maxSpeed * _moveSpeedMultiplier;
    }
    public float ActiveTempMultiplier => _activeTempMultiplier;

}
