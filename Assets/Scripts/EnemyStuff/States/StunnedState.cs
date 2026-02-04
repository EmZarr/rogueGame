using UnityEngine;
using UnityEngine.AI;

public class StunnedState : BaseState
{
    private readonly NavMeshAgent _agent;
    private readonly MonoBehaviour _attackBehaviour;

    private bool _prevStopped;

    public StunnedState(Enemy enemy) : base(enemy)
    {
        _agent = enemy.GetAgent();

        _attackBehaviour = enemy.GetAttack() as MonoBehaviour; 
    }

    public override void EnterState()
    {
        if (_agent)
        {
            _prevStopped = _agent.isStopped;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        if (_attackBehaviour)
            _attackBehaviour.enabled = false;
    }

    public override void Execute()
    {
        Debug.Log("fuck i am stunned");
        // intentionally empty
    }

    public override void ExitState()
    {
        if (_agent)
            _agent.isStopped = _prevStopped;

        if (_attackBehaviour)
            _attackBehaviour.enabled = true;
    }

    public override BaseState GetNextState()
    {
        if (_enemy.IsStunned)
            return this;

        return new IdleState(_enemy);
    }
}
