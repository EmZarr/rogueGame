using UnityEngine;
using UnityEngine.AI;

public abstract class BaseState
{
    protected Enemy _enemy;
    protected NavMeshAgent _agent;
    protected GameObject _player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected BaseState(Enemy enemy)
    {
        _enemy = enemy;
        _agent = enemy.GetAgent();
        _player = enemy.GetPlayer();
    }
    protected bool EnsurePlayer()
    {
        if (_player != null && _player.activeInHierarchy) return true;
        _player = GameObject.FindGameObjectWithTag("Player");
        return _player != null && _player.activeInHierarchy;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void Execute();
    public abstract BaseState GetNextState();
}
