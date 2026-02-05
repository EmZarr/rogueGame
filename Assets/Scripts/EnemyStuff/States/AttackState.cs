using System.Collections;
using UnityEngine;

public class AttackState : BaseState
{
    private bool _attackInProgress;

    public AttackState(Enemy enemy) : base(enemy) { }

    public override void EnterState()
    {
        _attackInProgress = false;

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    public override void Execute()
    {
        if (_attackInProgress)
            return;

        if (!EnsurePlayer())
            return;

        if (!_enemy.GetAttack().IsReady())
            return;

        _attackInProgress = true;
        _enemy.StartCoroutine(AttackFlow());
    }

    private IEnumerator AttackFlow()
    {
        yield return _enemy.GetAttack().Attack();
        _attackInProgress = false;
    }

    public override void ExitState()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.isStopped = false;
    }

    public override BaseState GetNextState()
    {
        if (!EnsurePlayer())
            return new IdleState(_enemy);

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return new IdleState(_enemy);

        if (_enemy._data.enemyType == EnemyType.Bomber && _enemy._currentHealth < _enemy._data.health / 2)
        {
            return new SuicideState(_enemy);
        }

        float dist = Vector3.Distance(_agent.transform.position, _player.transform.position);

        if(_enemy._data.enemyType == EnemyType.Ranged && dist < (_enemy.GetAttackRange() / 2) && _enemy.canDash)
        {
            _enemy.Dash();
        }
        
        if (!_attackInProgress && (dist > _enemy.GetAttackRange() || !_enemy.GetAttack().IsReady()))
        {
            if (_enemy._data.enemyType == EnemyType.Assassin)
            {
                return new OutflankState(_enemy);
            }
            else
            {
                return new GetInRangeState(_enemy);
            }
        }

        return this;
    }
}
