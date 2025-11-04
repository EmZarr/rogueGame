using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttackZone : MonoBehaviour
{

    private bool _active = false;
    private Collider2D _areaCollider;
    private SpriteRenderer _spriteRenderer;
    Vector3 _originalScale;
    Color _originalColor;
    private void Awake()
    {
        _areaCollider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.enabled = false;
        _originalScale = transform.localScale;
        _originalColor = _spriteRenderer.color;
    }

    public void Activate(Vector2 mousePos)
    {

        // Face toward mouse
        Vector2 dir = (mousePos - (Vector2)transform.parent.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float offSet = 1.4f;

        // Position the zone 1.4 units from the parent 
        transform.position = transform.parent.position + (Vector3)dir * offSet;

        // Rotate to face the player (adjust +90 since sprite points "up")
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        _active = true;
        _spriteRenderer.enabled = true;
    }



    public void Deactivate()
    {
        _active = false;
        _spriteRenderer.enabled = false;
    }

    public IEnumerator DealDamage(float damage)
    {
        SetAlpha(1f);
        yield return new WaitForSeconds(0.1f);
        SetAlpha(0.1f);

        var results = new List<Collider2D>();
        Physics2D.OverlapCollider(_areaCollider, results);

        foreach (var hit in results)
        {
            if (hit.TryGetComponent<Enemy>(out Enemy enemy))
            {
                
                enemy.TakeDamage(damage);
            }
        }

        // Restore
        transform.localScale = _originalScale;
        _spriteRenderer.color = _originalColor;
    }

    private void SetAlpha(float a)
    {
        var c = _spriteRenderer.color;
        c.a = a;
        _spriteRenderer.color = c;
    }


}
