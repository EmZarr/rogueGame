using System;
using UnityEngine;

public class GuardianProtectZone : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    private float _duration;

    private Action _onFinished;

    private GameObject _player;

    public void Activate(float duration, float delay, Action onFinished = null)
    {
        _duration = duration;
        _onFinished = onFinished;
        _player = GameObject.FindWithTag("Player");

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
        var projectile = other.GetComponent<TwoXArrowLogic>();
        if (projectile != null)
        {
            projectile.Init(5f, (Vector2) _player.transform.position, false, true);
        }
    }

     private void SetAlpha(float a)
    {
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}