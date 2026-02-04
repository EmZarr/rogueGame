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

    public bool knockBack = false;

    public void Activate(float dmg, float duration, float delay, Action onFinished = null)
    {
        _dmg = dmg;
        _duration = duration;
        _hit = false;
        _onFinished = onFinished;

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

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player != null && !_hit)
        {
            player.TakeDamage(_dmg, transform.parent.gameObject);
            if (knockBack)
            {
                var direction = other.transform.position - transform.position;
                player.GetKnockedBack(direction, 4.0f);
            }
            _hit = true;
        }
    }

    private void SetAlpha(float a)
    {
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
