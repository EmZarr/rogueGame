using UnityEngine;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    private float damage;
    private readonly HashSet<Enemy> hit = new();

    public void Activate(float dmg, float duration)
    {
        damage = dmg;
        hit.Clear();

        if (sr) sr.enabled = true;
        if (col) col.enabled = true;

        CancelInvoke();
        Invoke(nameof(Deactivate), duration);
    }

    void Deactivate()
    {
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var defenseZone = other.GetComponent<GuardianProtectZone>();
        if (defenseZone != null){
            Debug.Log("blocked");
        }
        else {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && hit.Add(enemy))
            enemy.TakeDamage(damage);
        }
    }
}
