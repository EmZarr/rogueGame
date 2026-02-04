using UnityEngine;
using UnityEngine.AI;

public class IdleState : BaseState
{
    private readonly float _radius;
    private readonly Vector2 _waitRange;

    private float _nextActionTime;
    private bool _hasDestination;

    public IdleState(Enemy enemy) : base(enemy)
    {
        _radius = _enemy.WanderRadius;
        _waitRange = _enemy.WanderWaitRange;
    }

    public override void EnterState()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.isStopped = false;

        _hasDestination = false;
        _nextActionTime = Time.time;
    }

    public override void Execute()
    {
        if (!_hasDestination && Time.time >= _nextActionTime)
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh && TryGetRandomPointAroundHome(out var dest))
            {
                dest.z = _enemy.HomePosition.z;
                _agent.SetDestination(dest);
                _hasDestination = true;
            }
            else
            {
                _nextActionTime = Time.time + 0.3f;
            }
        }

        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh && _hasDestination && !_agent.pathPending)
        {
            if (_agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                _hasDestination = false;
                _nextActionTime = Time.time + Random.Range(_waitRange.x, _waitRange.y);
            }
        }
    }

    public override void ExitState()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.isStopped = false;
    }

    public override BaseState GetNextState()
    {
        if (!EnsurePlayer())
            return this;

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return this;

        if (Vector3.Distance(_agent.transform.position, _player.transform.position) < _enemy.GetChaseRange())
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

    private bool TryGetRandomPointAroundHome(out Vector3 result)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 r = Random.insideUnitCircle * _radius;
            Vector3 candidate = _enemy.HomePosition + new Vector3(r.x, r.y, 0f);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}
