using UnityEngine;
using UnityEngine.AI;

public abstract class BaseState
{
    protected Enemy _enemy;
    protected NavMeshAgent _agent;
    protected Player _player => _enemy.GetPlayer();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected BaseState(Enemy enemy)
    {
        _enemy = enemy;
        _agent = enemy.GetAgent();
    }
    protected bool EnsurePlayer()
    {
        return _player != null;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void Execute();
    public abstract BaseState GetNextState();

    protected bool HasLineOfSight()
    {
        Vector2 origin = _agent.transform.position;
        Vector2 target = _player.transform.position;

        Vector2 direction = (target - origin);
        float distance = direction.magnitude;
        direction.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, LayerMask.GetMask("Wall"));

        // If ray hit a wall before reaching player -> blocked
        if (hit.collider != null)
            return false;

        return true;
    }
}
