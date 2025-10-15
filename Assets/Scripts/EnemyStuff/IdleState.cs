using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(Enemy enemy) : base(enemy)
    {
    }

    public override void EnterState()
    {
        _agent.isStopped = true;
    }

    public override void Execute()
    {
    }

    public override void ExitState()
    {
        _agent.isStopped = false;
    }

    public override BaseState GetNextState()
    {
        if (Vector3.Distance(_agent.transform.position, _player.transform.position) < _enemy.GetChaseRange())
        {
            return new GetInRangeState(_enemy);  // Transition to chase if player is detected
        }
        return this;
    }
}