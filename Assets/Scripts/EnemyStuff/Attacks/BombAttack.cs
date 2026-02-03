using System.Collections;
using UnityEngine;

public class BombAttack : IAttack
{
    [SerializeField] private GameObject bombPrefab;
    private float speed = 20f;
    /*protected override IEnumerator SpecialAttack()
    {
        // Windup, make attack, wind down
        yield return new WaitForSeconds(_attackSpeed / 2);
        var proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        proj.SetInstigator(gameObject);
        proj.Init(true, _damage*2);
        yield return new WaitForSeconds(_attackSpeed / 2);
    }*/
    protected override IEnumerator BasicAttack()
    {
        yield return new WaitForSeconds(_attackDelay);
        var bombObj = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        var bomb = bombObj.GetComponent<Bomb>();
        bomb.GetThrown(_player.transform.position, speed, _attackDelay, _damage);
        _nextReadyTime = Time.time + _attackSpeed;
    }

}
