using UnityEngine;

public class GetInRangeState : BaseState
{
    public GetInRangeState(Enemy enemy) : base(enemy)
    {

    }

    public override void EnterState()
    {
    }

    public override void Execute()
    {
        _agent.SetDestination(_player.transform.position);

    }

    public override void ExitState()
    {
    }

    public override BaseState GetNextState()
    {
        if (Vector3.Distance(_agent.transform.position, _player.transform.position) > _enemy.GetChaseRange())
        {
            return new IdleState(_enemy);  // Transition to chase if player is detected
        }
        else if (Vector3.Distance(_agent.transform.position, _player.transform.position) < _enemy.GetAttackRange())
        {
            return new AttackState(_enemy);
        }
        return this;  // Remain in Chase
    }
}
