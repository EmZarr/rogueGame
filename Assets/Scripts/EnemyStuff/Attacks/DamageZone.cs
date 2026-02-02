using System;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    private float _dmg;
    private float _duration;
    private bool _hit;
    private Action _onFinished;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player != null && !_hit)
        {
            player.TakeDamage(_dmg, transform.parent.gameObject);
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
