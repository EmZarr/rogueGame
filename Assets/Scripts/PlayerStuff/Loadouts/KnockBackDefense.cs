
using UnityEngine;

public class KnockBackDefense : SwordHitbox
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            var direction = enemy.transform.position - transform.parent.position;
            enemy.GetKnockedBack(direction, 3.0f);
        }
    }
}