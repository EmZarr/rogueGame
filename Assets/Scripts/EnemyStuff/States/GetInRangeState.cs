using UnityEngine;
using UnityEngine.AI;

public class GetInRangeState : BaseState
{
    public GetInRangeState(Enemy enemy) : base(enemy) { }

    private float _nextRepathTime;
    private float repathInterval = 0.15f;

    public override void EnterState()
    {
    }

    public override void Execute()
    {
        if (!EnsurePlayer()) return;
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        Vector3 toPlayer = _player.transform.position - _agent.transform.position;
        toPlayer.y = 0f;

        // Guardian: if player is more than 90 degrees off the front,
        // only rotate, do not move.
        if (_enemy._data.enemyType == EnemyType.Guardian || toPlayer.sqrMagnitude < 0.001f)
        {
            float angle = Vector3.Angle(_agent.transform.forward, toPlayer.normalized);

            if (angle > 90f)
            {
                _agent.isStopped = true;

                Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
                _agent.transform.rotation = Quaternion.RotateTowards(
                    _agent.transform.rotation,
                    targetRotation,
                    _agent.angularSpeed * Time.deltaTime
                );

                return;
            }
        }

        if (_enemy._data.enemyType == EnemyType.Bomber) return;

        _agent.isStopped = false;

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

        if (_enemy._data.enemyType == EnemyType.Bomber && _enemy._currentHealth < _enemy._data.health / 4)
        {
            return new SuicideState(_enemy);
        }

        float dist = Vector3.Distance(
            _agent.transform.position,
            _player.transform.position
        );

        if (dist > _enemy.GetChaseRange())
            return new IdleState(_enemy);
        if (dist < _enemy.GetAttackRange() && _enemy.GetAttack().IsReady() && HasLineOfSight())
            return new AttackState(_enemy);

        return this;
    }
}