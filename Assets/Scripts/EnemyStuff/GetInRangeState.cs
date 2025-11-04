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
        if (_player == null || !_player.activeInHierarchy)
            return;
        _agent.SetDestination(_player.transform.position);
        

    }

    public override void ExitState()
    {
    }

    public override BaseState GetNextState()
    {
        if (_player == null || !_player.activeInHierarchy)
            return new IdleState(_enemy);

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
