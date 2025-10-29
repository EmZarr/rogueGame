using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meleeDamageZone : MonoBehaviour
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

    public void Activate()
    {

        // Face toward player
        GameObject player = GameObject.FindWithTag("Player");
        if (player)
        {
            Vector2 dir = (player.transform.position - transform.parent.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float offSet = 1.4f;

            // Position the zone 1.4 units from the parent toward the player (world space)
            transform.position = transform.parent.position + (Vector3)dir * offSet;

            // Rotate to face the player (adjust +90 since sprite points "up")
            transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
        }

        _active = true;
        _spriteRenderer.enabled = true;
    }



    public void Deactivate()
    {
        _active = false;
        _spriteRenderer.enabled = false;
    }

    public IEnumerator DealDamage(float damage, bool isSpecial)
    {
        SetAlpha(1f);
        yield return new WaitForSeconds(0.1f);
        SetAlpha(0.1f);

        var results = new List<Collider2D>();
        Physics2D.OverlapCollider(_areaCollider, results);

        foreach (var hit in results)
        {
            if (hit.TryGetComponent<Player>(out Player player))
            {
                // If special, can't block
                if (!isSpecial)
                {
                    // Check if a shield is between enemy and player
                    Vector2 origin = transform.parent.position;
                    Vector2 target = player.transform.position;
                    Vector2 dir = (target - origin).normalized;
                    float dist = Vector2.Distance(origin, target);

                    // Perform a linecast between enemy and player
                    RaycastHit2D hitInfo = Physics2D.Linecast(origin, target, LayerMask.GetMask("Shield"));

                    if (hitInfo.collider != null)
                    {
                        // If we hit a shield before hitting the player, cancel damage
                        if (hitInfo.collider.CompareTag("Shield"))
                        {
                            continue;
                        }
                    }
                }

                // If no shield blocks the path, deal damage
                player.TakeDamage(damage);
            }
        }

        // Restore
        transform.localScale = _originalScale;
        _spriteRenderer.color = _originalColor;
    }

    public void ActivateSpecial()
    {
        // Apply special look
        transform.localScale = _originalScale * 2f;
        var c = Color.red;
        c.a = 0.1f;
        _spriteRenderer.color = c;

        // Face toward player
        GameObject player = GameObject.FindWithTag("Player");
        if (player)
        {
            Vector2 dir = (player.transform.position - transform.parent.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float offSet = 2.8f;

            // Position the zone 1.4 units from the parent toward the player (world space)
            transform.position = transform.parent.position + (Vector3)dir * offSet;

            // Rotate to face the player (adjust +90 since sprite points "up")
            transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
        }

        _active = true;
        _spriteRenderer.enabled = true;
    }

    private void SetAlpha(float a)
    {
        var c = _spriteRenderer.color;
        c.a = a;
        _spriteRenderer.color = c;
    }


}
