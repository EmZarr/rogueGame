using UnityEngine;
using UnityEngine.AI;

public class OutflankState : BaseState
{
    public OutflankState(Enemy enemy) : base(enemy) { }

    private float distanceToPlayer = 5f;
    private readonly float circleFatness = 1f;

    private readonly float radialCorrectionStrength = 1.5f;
    private readonly float lookAhead = 2.0f;

    private float _orbitSign;
    private float timer = 0f;

    // Reuse one path object to avoid GC allocations every frame
    private readonly NavMeshPath _path = new NavMeshPath();

    public override void EnterState()
    {
        timer = 0f;
        _orbitSign = (Random.value < 0.5f) ? -1f : 1f;
        distanceToPlayer = Random.Range(4f, 8f);
        _enemy._data.attackSpeed = Random.Range(_enemy._data.attackSpeed, _enemy._data.attackSpeed + 2f);
    }

    public override void Execute()
    {
        if (!EnsurePlayer())
            return;

        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;

        timer += Time.deltaTime;

        Vector2 enemyPos = _agent.transform.position;
        Vector2 playerPos = _player.transform.position;

        Vector2 toEnemy = enemyPos - playerPos;
        float dist = toEnemy.magnitude;

        if (dist < 0.001f)
            return;

        Vector2 radial = toEnemy / dist;

        // Perpendicular (tangent) in XY plane
        Vector2 tangent = new Vector2(-radial.y, radial.x) * _orbitSign;

        float minDist = distanceToPlayer - circleFatness;
        float maxDist = distanceToPlayer + circleFatness;

        float radialError = 0f;
        if (dist < minDist) radialError = (minDist - dist);
        else if (dist > maxDist) radialError = -(dist - maxDist);

        Vector2 correction = radial * (radialError * radialCorrectionStrength);

        Vector2 desiredDir = (tangent + correction).normalized;

        // Try a primary destination (full lookAhead), then a fallback (shorter step)
        if (!TrySetOrbitDestination(enemyPos, playerPos, desiredDir, minDist, maxDist, lookAhead))
        {
            // fallback: flip orbit direction
            _orbitSign *= -1f;

            // optional: also try a smaller step after flipping, helps in tight spaces
            Vector2 flippedTangent = new Vector2(-radial.y, radial.x) * _orbitSign;
            Vector2 flippedDir = (flippedTangent + correction).normalized;

            TrySetOrbitDestination(enemyPos, playerPos, flippedDir, minDist, maxDist, lookAhead * 0.5f);
        }
    }

    private bool TrySetOrbitDestination(
        Vector2 enemyPos,
        Vector2 playerPos,
        Vector2 desiredDir,
        float minDist,
        float maxDist,
        float step)
    {
        Vector2 destination2D = enemyPos + desiredDir * step;

        // Clamp destination back onto the ring band
        Vector2 fromPlayer = destination2D - playerPos;
        float clampedRadius = Mathf.Clamp(fromPlayer.magnitude, minDist, maxDist);
        destination2D = playerPos + fromPlayer.normalized * clampedRadius;

        Vector3 desiredWorld = new Vector3(destination2D.x, destination2D.y, 0f);

        // 1) Snap candidate to navmesh
        if (!TryGetClosestPointOnNavMesh(desiredWorld, 1.5f, out Vector3 clamped))
            return false;

        // 2) NEW: Make sure there is a complete path to it (not behind a wall / disconnected)
        if (!HasCompletePath(_agent, clamped, _path))
            return false;

        _agent.SetDestination(clamped);
        return true;
    }

    public override void ExitState() { }

    public override BaseState GetNextState()
    {
        if (timer > _enemy._data.attackSpeed)
        {
            return new AttackState(_enemy);
        }

        float dist = Vector2.Distance(_agent.transform.position, _player.transform.position);

        if (dist > _enemy.GetChaseRange())
            return new IdleState(_enemy);

        return this;
    }

    public static bool TryGetClosestPointOnNavMesh(
         Vector3 desiredWorld,
         float maxDistance,
         out Vector3 closestWorld,
         int areaMask = NavMesh.AllAreas)
    {
        if (NavMesh.SamplePosition(desiredWorld, out NavMeshHit hit, maxDistance, areaMask))
        {
            closestWorld = hit.position;
            return true;
        }

        closestWorld = desiredWorld;
        return false;
    }

    private static bool HasCompletePath(NavMeshAgent agent, Vector3 destination, NavMeshPath reusablePath)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        // Note: CalculatePath uses the agent's areaMask + settings implicitly.
        if (!agent.CalculatePath(destination, reusablePath))
            return false;

        return reusablePath.status == NavMeshPathStatus.PathComplete;
    }
}
