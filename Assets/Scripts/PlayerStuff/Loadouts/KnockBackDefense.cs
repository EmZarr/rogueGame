
using UnityEngine;

public class KnockBackDefense : SwordHitbox
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            var direction = enemy.transform.position - transform.parent.position;
            enemy.GetKnockedBack(direction, 5.0f);
            base.telemetryManager.defenseToEnemy[0, (int) enemy._data.enemyType]+=1;
        }
    }
}