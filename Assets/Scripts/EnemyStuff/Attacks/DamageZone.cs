using System;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer sr;
    [SerializeField] protected Collider2D col;

    protected float _dmg;
    protected float _duration;
    protected bool _hit = false;
    protected Action _onFinished;
    protected bool _isAllied = false;

    public float knockBackDistance = 0.0f;

    TelemetryManager telemetryManager;

    public void Activate(float dmg, float duration, float delay, Action onFinished = null)
    {
        _dmg = dmg;
        _duration = duration;
        _hit = false;
        _onFinished = onFinished;
        telemetryManager = FindAnyObjectByType<TelemetryManager>();

        if (sr) sr.enabled = true;
        SetAlpha(0.1f);

        CancelInvoke();
        Invoke(nameof(DealDamage), delay);
    }

    private void DealDamage()
    {
        SetAlpha(1f);
        if (col) col.enabled = true;

        CancelInvoke();
        Invoke(nameof(Deactivate), _duration);
    }

    private void Deactivate()
    {
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;

        _onFinished?.Invoke();
    }

    public void Cancel(bool invokeFinished = false)
    {
        CancelInvoke();

        if (sr) sr.enabled = false;
        if (col) col.enabled = false;

        _hit = false;

        if (invokeFinished)
            _onFinished?.Invoke();

        _onFinished = null;
    }

    private void OnDisable()
    {
        Cancel();
    }

    public void changeTeam()
    {
        _isAllied = !_isAllied;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isAllied)
        {
            if (other.TryGetComponent<Player>(out Player player) && !_hit)
            {
                player.TakeDamage(_dmg, transform.parent.gameObject);
                if (knockBackDistance > 0.0f)
                {
                    var direction = (other.transform.position - transform.position).normalized;
                    player.GetKnockedBack(direction, knockBackDistance);
                }
                _hit = true;
                telemetryManager.DamageTrack(0, _dmg);
            }
        }
        if (_isAllied)
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(_dmg);
                if (knockBackDistance > 0.0f)
                {
                    var direction = (other.transform.position - transform.position).normalized;
                    enemy.GetKnockedBack(direction, knockBackDistance);
                }
            }
        }
    }

    private void SetAlpha(float a)
    {
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
