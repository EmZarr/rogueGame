using System;

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    [SerializeField] private LayerMask _shieldMask;
    Shield _shield;
    UI _ui;
    float _score = 0;
    public event Action<GameObject /*killer*/> OnDied;

    private bool _isInvinsible = false;
    private bool _isStunning = false;
    private float _stunDuration = 1f;
    private bool _isDealingDmg = false;
    private float _damage = 5f;

    public float DamageAmp = 0.0f;
    public float BaseDamageAmp = 1f;

    public float AttackSpeedIncrease = 1f;

    public float HeavyDashCooldownDecrease = 1f;

    private float _damageBoostDuration;
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
            // Successful parry: negate damage + stun attacker (example)
            var enemy = attacker.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyStun(_parryStunDuration);
                Debug.Log("Stunned");
            }
            return;
        }

        if (_shield.isBlocking && shieldIntercepts)
        {
            return;
        }
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
        _damage = damageAmount*BaseDamageAmp + damageAmount*DamageAmp;
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

    void OnTriggerEnter2D(Collider2D other)
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

    private IEnumerator KnockbackRoutine(Vector3 direction, float distance)
    {
        float dashDuration = 0.25f;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * distance);

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.Raycast(start, end, out hit, UnityEngine.AI.NavMesh.AllAreas))
        {
            end = hit.position;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null; // wait one frame
        }
    }

    public void IncreaseDamage(float percent, bool permanent)
    {
        if (permanent){
            BaseDamageAmp += 0.01f*percent;
        }
        else
        {
            StartCoroutine(DamageAmpTimer(percent, 10.0f));
        }
    }
    private IEnumerator DamageAmpTimer(float percent, float time)
    {
        _damageBoostDuration += time;
        DamageAmp += 0.01f*percent;
        yield return new WaitForSeconds(_damageBoostDuration );
        _damageBoostDuration -= time;
        if (_damageBoostDuration <= 0)
        {
             DamageAmp = 0f;
        }
        
    }

    public void IncreaseAttackSpeed(float percentIncrease)
    {
        AttackSpeedIncrease -= 0.01f*percentIncrease;
    }

    public void DecreaseHeavyDashCooldown(float percentDecrease)
    {
        HeavyDashCooldownDecrease -= 0.01f*percentDecrease;
    }




}
