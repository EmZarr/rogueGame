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
    float _damageBlocked = 0;

    TelemetryManager telemetryManager;

    PopUpCreator popup;
    private Coroutine _tempBuffCo;
    public event Action<GameObject /*killer*/> OnDied;
    public static Player Instance { get; private set; }

    public Vector3 closestEnemy;

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
        // optional: ensure no stuck buff if object is disabled mid-buff
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

    private float _parryStunDuration = 1f;

    bool _isRespawning;

    private void Start()
    {
        _ui = FindAnyObjectByType<UI>();
        _shield = GetComponentInChildren<Shield>();
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
        popup = FindFirstObjectByType<PopUpCreator>();
    }

    void Update()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 30f, LayerMask.GetMask("Road"));
        float shortestDistance = 100f;
        foreach (var hit in hits)
        {
            Vector2 closestPoint = hit.ClosestPoint(transform.position);
            float distance = Vector2.Distance(transform.position, closestPoint);
            if (shortestDistance > distance)
            {
                shortestDistance = distance;
            }
        }
        if (shortestDistance < 100f)
        {
            telemetryManager.DistanceToPath(shortestDistance);
        }

        Collider2D[] wallHits = Physics2D.OverlapCircleAll(transform.position, 20f, LayerMask.GetMask("Wall"));
        shortestDistance = 100f;
        foreach (var hit in wallHits)
        {
            Vector2 closestPoint = hit.ClosestPoint(transform.position);
            float distance = Vector2.Distance(transform.position, closestPoint);
            if (shortestDistance > distance)
            {
                shortestDistance = distance;
            }
        }
        if (shortestDistance < 100f)
        {
            telemetryManager.DistanceToWall(shortestDistance);
        }


        Collider2D[] enemyHits = Physics2D.OverlapCircleAll(transform.position, 20f, LayerMask.GetMask("Enemies"));
        float totalDistance = 0f;
        var counter = 1;
        shortestDistance = 100f;
        foreach (var hit in enemyHits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            totalDistance += distance;
            counter++;
            if (shortestDistance > distance)
            {
                shortestDistance = distance;
                closestEnemy = hit.transform.position;
            }
        }
        totalDistance = totalDistance / counter;
        if (totalDistance > 0)
        {
            telemetryManager.DistanceToEnemy(totalDistance);
        }

    }
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (_isInvinsible)
        {
            telemetryManager.defenseToEnemy[telemetryManager.loadOutNumber, (int)attacker.GetComponentInParent<Enemy>()._data.enemyType] += 1;
            return;
        }

        bool shieldIntercepts = attacker != null && AttackLineHitsShield(attacker.transform);

        if (_shield.isParrying && shieldIntercepts)
        {
            var enemy = attacker.GetComponentInParent<Enemy>();
            if (enemy != null && enemy._data.enemyType != EnemyType.Ranged)
            {
                enemy.ApplyStun(_parryStunDuration);
                _loadoutState.RefundDefCD();
                telemetryManager.defenseToEnemy[telemetryManager.loadOutNumber, (int)enemy._data.enemyType] += 1;
            }
            return;
        }

        if (_shield.isBlocking && shieldIntercepts)
        {
            AddDamageBlocked(damage);
            telemetryManager.defenseToEnemy[telemetryManager.loadOutNumber, (int)attacker.GetComponentInParent<Enemy>()._data.enemyType] += 1;
            return;
        }
        _flash.Flash();
        _health -= damage;
        popup.CreatePopUp(damage, transform.position, 1);
        if (_health <= 0)
        {
            rb.linearVelocity = Vector2.zero;
            OnDied?.Invoke(attacker);
            return;
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        popup.CreatePopUp(scoreIncrease, transform.position, 5);
        telemetryManager.IncreaseCurrentMapScore(scoreIncrease);
        _ui.updateScore(_score);
    }

    public float GetScore()
    {
        return _score;
    }

    public void Heal(float health)
    {

        _health += health;
        if (_health > 150)
        {
            _health = 150;
            popup.CreatePopUp(0, transform.position, 3);
        }
        else
        {
            popup.CreatePopUp(health, transform.position, 3);
        }
        _healthAnimator.SetTrigger("PickUpHealth");
        _ui.updateHealth(_health);

    }

    public void TeleportTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
    IEnumerator RespawnRoutine()
    {
        _isRespawning = true;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        yield return new WaitForSeconds(0.2f);
        ResetStats();
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
        _damage = damageAmount * _damageMultiplier;
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
                telemetryManager.loadoutToEnemy[1, 2, (int)enemy._data.enemyType] += 1;
            }
            if (_isDealingDmg)
            {
                enemy.TakeDamage(_damage);
                telemetryManager.loadoutToEnemy[2, 2, (int)enemy._data.enemyType] += 1;
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
            if (_isRespawning) break;
            // override velocity so damping/mass doesn't make it "barely move"
            rb.linearVelocity = dir * speed;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        _loadoutState.SetMovementBlocked(false);
    }


    public void IncreaseMovespeed(float increase)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _moveSpeedMultiplier += increase;
        _loadoutState.SetSpeed(1f);
        popup.CreatePopUp(increase, transform.position, 4);
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }
    public void IncreaseAttackSpeed(float increase)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _attackSpeedMultiplier += increase;
        popup.CreatePopUp(increase, transform.position, 4);
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }
    public void IncreaseDamage(float increase)
    {
        _powerUpAnimator.SetTrigger("PowerPickUp");
        _damageMultiplier += increase;
        popup.CreatePopUp(increase, transform.position, 4);
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
        HeavyDashCooldownDecrease -= 0.01f * percentDecrease;
    }

    public bool IsInvinsible()
    {
        return _isInvinsible;
    }

    public void AddDamageBlocked(float dmg)
    {
        _damageBlocked += dmg;
    }

    public float GetDamageBlocked()
    {
        return _damageBlocked; 
    }

    public void ResetDamageBlocked()
    {
        _damageBlocked = 0f;
    }

    public Vector2 GetMouseDir()
    {
        return _loadoutState.getMouseDir();
    }

    public void ResetStats()
    {
        _damageMultiplier = 1.0f;
        _attackSpeedMultiplier = 1.0f;
        _moveSpeedMultiplier = 1.0f;
        _activeTempMultiplier = 1f;
        HeavyDashCooldownDecrease = 1f;
        _loadoutState.SetSpeed(1f);
        _health = 150;
        _ui.updateHealth(_health);
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);

    }

    public void RefreshUI()
    {
        _ui.updateHealth(_health);
        _ui.updateScore(_score);
        _ui.updateBuffs(_attackSpeedMultiplier, _moveSpeedMultiplier, _damageMultiplier);
    }

    public void ResetScore()
    {
        _score = 0;
        _ui.updateScore(_score);
    }
    public float DamageMultiplier => _damageMultiplier;
    public float AttackSpeedMultiplier => _attackSpeedMultiplier;
    public float GetMoveSpeed()
    {
        return _maxSpeed * _moveSpeedMultiplier;
    }
    public float MovementSpeedMultiplier => _moveSpeedMultiplier;
    public float ActiveTempMultiplier => _activeTempMultiplier;

}