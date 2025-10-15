using System.Collections;
using UnityEngine;

public class AttackState : BaseState
{
    private bool _attackInProgress;
    public AttackState(Enemy enemy) : base(enemy)
    {

    }
    public override void EnterState()
    {
        _attackInProgress = false;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    public override void Execute()
    {
        if (_attackInProgress) return;

        _attackInProgress = true;
        // Enemy is a MonoBehaviour; use it to run coroutines
        _enemy.StartCoroutine(AttackFlow());
    }

    private IEnumerator AttackFlow()
    {
        yield return _enemy.GetAttack().Attack();
        _attackInProgress = false;
    }

    public override void ExitState()
    {
        _agent.isStopped = false;
    }

    public override BaseState GetNextState()
    {
        if (Vector3.Distance(_agent.transform.position, _player.transform.position) > _enemy.GetAttackRange() && !_attackInProgress)
        {
            return new GetInRangeState(_enemy); 
        }
        return this;
    }


}
