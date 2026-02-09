using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    [Header("Shader")]
    [SerializeField] private string frontProp = "_Front";
    [SerializeField] private string alphaProp = "_Alpha";

    private float damage;
    private readonly HashSet<Enemy> hit = new();

    private MaterialPropertyBlock _mpb;
    private Coroutine _animRoutine;

    private int _frontID;
    private int _alphaID;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _frontID = Shader.PropertyToID(frontProp);
        _alphaID = Shader.PropertyToID(alphaProp);

        // start disabled if you want
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
    }

    public void Activate(float dmg, float duration)
    {
        damage = dmg;
        hit.Clear();

        if (sr) sr.enabled = true;
        if (col) col.enabled = true;

        // reset + animate the shader from HERE
        if (sr)
        {
            // reset instantly so it starts at center on enable
            sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_frontID, 0f);
            _mpb.SetFloat(_alphaID, 1f);
            sr.SetPropertyBlock(_mpb);

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(AnimateWave(duration));
        }

        CancelInvoke();
        Invoke(nameof(Deactivate), duration);
    }

    private IEnumerator AnimateWave(float duration)
    {
        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration); // 0..1

            sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_frontID, n);
            sr.SetPropertyBlock(_mpb);

            yield return null;
        }

        // ensure it ends cleanly
        sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_frontID, 1f);
        _mpb.SetFloat(_alphaID, 0f);
        sr.SetPropertyBlock(_mpb);
    }

    void Deactivate()
    {
        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var defenseZone = other.GetComponent<GuardianProtectZone>();
        if (defenseZone != null)
        {
            defenseZone.TakeDamage(damage);
        }
        else
        {
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && hit.Add(enemy))
                enemy.TakeDamage(damage);
        }
    }
}
