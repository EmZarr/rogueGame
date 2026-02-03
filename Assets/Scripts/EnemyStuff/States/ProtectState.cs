using System.Collections;
using UnityEngine;

public class ProtectState : BaseState
{
    public bool isProtecting;
    public ProtectState(Enemy enemy) : base(enemy){}

    public override void EnterState()
    {
        isProtecting = true;
    }

    public override void Execute()
    {
        if (!EnsurePlayer())
            return;
        if (_enemy.canProtect)
        {
            Debug.Log("I protect");
            _enemy.StartCoroutine(ProtectTimer());
        }

        if (isProtecting)
        {
            _enemy.StartCoroutine(ProtectFlow());
        }
        
        
    }

    private IEnumerator ProtectFlow()
    {
        yield return _enemy.GetProtect().Protect();
        //_attackInProgress = false;
    }

    private IEnumerator ProtectTimer()
    {
        _enemy.canProtect = false;
        yield return new WaitForSeconds(5.0f);
        isProtecting = false;
        yield return new WaitForSeconds(_enemy._data.protectCooldown);
        _enemy.canProtect = true;
        
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


        float dist = Vector3.Distance(_agent.transform.position, _player.transform.position);

        if (dist > _enemy.GetAttackRange() && !isProtecting)
        {
             return new GetInRangeState(_enemy);
        }
           
        return this;
    }

}