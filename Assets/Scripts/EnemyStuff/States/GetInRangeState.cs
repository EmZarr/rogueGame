using UnityEngine;

public class GetInRangeState : BaseState
{
    public GetInRangeState(Enemy enemy) : base(enemy) { }

    public override void EnterState() { }

    public override void Execute()
    {
        if (!EnsurePlayer())
            return;

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        float dist = Vector3.Distance(_agent.transform.position, _player.transform.position);
        Vector3 direction = _agent.transform.position - _player.transform.position; 
        direction.Normalize(); 
        _agent.SetDestination(_player.transform.position + direction * (_enemy.GetAttackRange()-1f));
    }

    public override void ExitState() { }

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
        float dist = Vector3.Distance(
            _agent.transform.position,
            _player.transform.position
        );

        if (dist > _enemy.GetChaseRange())
            return new IdleState(_enemy);
        if (dist < _enemy.GetAttackRange() && _enemy.GetAttack().IsReady())
            return new AttackState(_enemy);
        return this;
    }
}
