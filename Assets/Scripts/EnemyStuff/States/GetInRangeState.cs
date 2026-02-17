using UnityEngine;
using UnityEngine.AI;

public class GetInRangeState : BaseState
{
    public GetInRangeState(Enemy enemy) : base(enemy) { }
    private float _nextRepathTime;
    private float repathInterval = 0.15f;

    public override void EnterState() {
    
    }

    public override void Execute()
    {
        if (!EnsurePlayer()) return;
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;
        if (Time.time < _nextRepathTime) return;
        _nextRepathTime = Time.time + repathInterval;

        Vector3 playerPos = _player.transform.position;
        float desiredRange = _enemy.GetAttackRange() - 1f;

        Vector3 dir = (_agent.transform.position - playerPos).normalized;
        Vector3 desiredPoint = playerPos + dir * desiredRange;

        // snap it onto navmesh
        if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, 2.0f, _agent.areaMask))
        {
            NavMeshPath path = new NavMeshPath();

            if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                _agent.SetDestination(hit.position);
            }
            else
            {
                _agent.SetDestination(playerPos);
            }
        }
        else
        {
            // fallback
            _agent.SetDestination(playerPos);
        }

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
