using System.Collections;
using UnityEngine;

public abstract class IAttack : MonoBehaviour
{
    [SerializeField] EnemyData _data;
    protected Player _player;
    protected float _nextReadyTime = 0f;
    protected float _attackSpeed;
    protected float _damage;
    protected float _attackDelay;


    private void Start()
    {
        _damage = _data.damage;
        _attackSpeed = _data.attackSpeed;
        _attackDelay = _data.attackDelay;
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }
    
    public IEnumerator Attack()
    {
        // Keep specials?
        /*if (IsReady())
        {
            yield return SpecialAttack();   // must yield until the special is done
            _nextReadyTime = Time.time + _cooldown;
        }
        else
        {
            yield return BasicAttack();     // must yield until the basic is done
        }*/
        if (IsReady())
        {
            yield return BasicAttack();
        }

    }
    public bool IsReady() { return Time.time >= _nextReadyTime; }
    //protected abstract IEnumerator SpecialAttack();
    protected virtual IEnumerator BasicAttack() {

        Debug.Log("Do cool attack");
        yield return _attackSpeed;
    }

    protected void TransformFacePlayer(Transform toMoveAndRotate, float distance, float angleOffset = 90f, float turnSpeedDegPerSec = 0f)
    {
        if (_player == null || toMoveAndRotate == null)
            return;

        Transform parent = toMoveAndRotate.parent;
        if (parent == null)
            return;

        Vector2 from = parent.position;
        Vector2 to = _player.transform.position;

        Vector2 dir = to - from;
        if (dir.sqrMagnitude < 0.000001f)
            return;

        dir.Normalize();

        // Position at distance from parent
        Vector3 pos = parent.position + (Vector3)(dir * distance);
        pos.z = toMoveAndRotate.position.z;
        if (turnSpeedDegPerSec <= 0f)
        {
            toMoveAndRotate.position = pos;
        }
        else
        {
            toMoveAndRotate.position = Vector3.MoveTowards(
                toMoveAndRotate.position,
                pos,
                turnSpeedDegPerSec * 0.1f * Time.deltaTime
            );
        }

        // Rotate to face player
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle + angleOffset);
        if (turnSpeedDegPerSec <= 0f)
        {
            toMoveAndRotate.rotation = targetRot;
        }
        else
        {
            toMoveAndRotate.rotation = Quaternion.RotateTowards(
                toMoveAndRotate.rotation,
                targetRot,
                turnSpeedDegPerSec * Time.deltaTime
            );
        }
    }
}
