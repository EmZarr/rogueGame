using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SwordHitbox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] protected SpriteRenderer sr;
    [SerializeField] protected Collider2D col;

    [SerializeField] private Vector2 colliderOffsetAtFront0 = new Vector2(-2.25f, 0f);  // where hitbox is when _Front = 0
    [SerializeField] private Vector2 colliderOffsetAtFront1 = new Vector2(2.25f, 0f); // where hitbox is when _Front = 1
    [SerializeField] private float widthAtFront0 = 0.1f;
    [SerializeField] private float widthAtFront1 = 1.0f;

    [Header("Shader")]
    [SerializeField] protected string frontProp = "_Front";
    [SerializeField] protected string alphaProp = "_Alpha";

    protected float damage;
    protected readonly HashSet<Enemy> hit = new();

    protected MaterialPropertyBlock _mpb;
    protected Coroutine _animRoutine;

    protected int _frontID;
    protected int _alphaID;

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

            if (col is BoxCollider2D box)
            {
                Vector2 target = Vector2.Lerp(colliderOffsetAtFront0, colliderOffsetAtFront1, n);
                col.offset = target;
                Vector2 s = box.size;
                s.x = Mathf.Lerp(widthAtFront0, widthAtFront1, n);
                box.size = s;
            }
            else if (col is CircleCollider2D cir)
            {
                cir.radius = Mathf.Lerp(widthAtFront0, widthAtFront1, Mathf.Clamp01(n / 0.7f));
            }
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

    protected virtual void OnTriggerEnter2D(Collider2D other)
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
