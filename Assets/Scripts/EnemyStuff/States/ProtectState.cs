using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ProtectState : BaseState
{
    public bool isProtecting;

    List<Enemy> toProtect;
    public ProtectState(Enemy enemy) : base(enemy){
        toProtect = new List<Enemy>();
    }

    public override void EnterState()
    {

        Collider2D[] hits = Physics2D.OverlapCircleAll(_enemy.transform.position, 10f,LayerMask.GetMask("Enemies"));
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null)
                continue;

            if (enemy._data.enemyType == EnemyType.Ranged || enemy._data.enemyType == EnemyType.Bomber)
            {
                toProtect.Add(enemy);
            }
        }
    }

    public override void Execute()
    {
        if (!EnsurePlayer())
            return;
        toProtect.RemoveAll(e => e == null);

        if (toProtect.Count > 0) {
            var chosen = ChooseEnemy();

            Vector2 directionToPlayer = (_player.transform.position - chosen.transform.position).normalized;
            Vector2 targetPos = (Vector2)chosen.transform.position + directionToPlayer * 2f;

            _agent.SetDestination(targetPos);
        }
    }

    public override void ExitState()
    {
        
    }

    public override BaseState GetNextState()
    {
        if (!EnsurePlayer())
            return new IdleState(_enemy);

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return new IdleState(_enemy);

        if (toProtect.Count < 1)
        {
            _agent.speed = 4f;
             return new GetInRangeState(_enemy);
        }
        return this;
    }

    private Enemy ChooseEnemy()
    {
        return toProtect
            .Where(e => e != null)
            .OrderBy(e => Vector2.Distance(e.transform.position, _player.transform.position))
            .FirstOrDefault();
    }

}