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
    private float _lastOrbitFlipTime = 0f;
    private const float OrbitFlipCooldown = 0.5f;
    private float attackTimer = 5f;
    private float timer = 0f;

    public override void EnterState()
    {
        timer = 0f;
        _orbitSign = (Random.value < 0.5f) ? -1f : 1f;
        distanceToPlayer = Random.Range(4f, 8f);
        attackTimer = Random.Range(5f, 9f);
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

        Vector2 destination2D = enemyPos + desiredDir * lookAhead;

        // Clamp destination back onto the ring band
        Vector2 fromPlayer = destination2D - playerPos;
        float clampedRadius = Mathf.Clamp(fromPlayer.magnitude, minDist, maxDist);
        destination2D = playerPos + fromPlayer.normalized * clampedRadius;

        Vector3 desiredWorld = new Vector3(destination2D.x, destination2D.y, 0f);

        if (TryGetClosestPointOnNavMesh(desiredWorld, 1.5f, out Vector3 clamped))
        {
            _agent.SetDestination(clamped);
        }
        else
        {
            // fallback: flip orbit
            _orbitSign *= -1f;
        }
    }

    public override void ExitState() { }

    public override BaseState GetNextState()
    {
        if (timer > attackTimer)
        {
            return new AttackState(_enemy);
        }

        float dist = Vector2.Distance(
            _agent.transform.position,
            _player.transform.position
        );

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
}

