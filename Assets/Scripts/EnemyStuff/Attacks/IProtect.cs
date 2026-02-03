using System.Collections;
using UnityEngine;

public abstract class IProtect : MonoBehaviour
{
    [SerializeField] EnemyData _data;
    protected Player _player;
    protected float _nextReadyTime = 0f;
    protected float _attackSpeed;
    protected float _damage;
    protected float _attackDelay;
    protected float _protectCooldown;


    private void Start()
    {
        _damage = _data.damage;
        _attackSpeed = _data.attackSpeed;
        _attackDelay = _data.attackDelay;
        _protectCooldown = _data.protectCooldown;
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    public bool IsReady() { return Time.time >= _nextReadyTime; }
    //protected abstract IEnumerator SpecialAttack();
    protected virtual IEnumerator BasicProtect() {

        yield return _attackSpeed;
    }

    public IEnumerator Protect()
    {
        if (IsReady())
        {
            yield return BasicProtect();
        }
    }

    protected void TransformFacePlayer(Transform toMoveAndRotate, float distance, float angleOffset = 90f)
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
        toMoveAndRotate.position = pos;

        // Rotate to face player
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        toMoveAndRotate.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }
}
