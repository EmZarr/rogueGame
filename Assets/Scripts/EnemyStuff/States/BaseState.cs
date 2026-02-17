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
}
