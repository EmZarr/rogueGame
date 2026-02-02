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

        _agent.SetDestination(_player.transform.position);
    }

    public override void ExitState() { }

    public override BaseState GetNextState()
    {
        if (!EnsurePlayer())
            return new IdleState(_enemy);

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return new IdleState(_enemy);

        float dist = Vector3.Distance(
            _agent.transform.position,
            _player.transform.position
        );

        if (dist > _enemy.GetChaseRange())
            return new IdleState(_enemy);

        if (dist < _enemy.GetAttackRange())
            return new AttackState(_enemy);

        return this;
    }
}
