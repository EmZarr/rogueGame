using UnityEngine;


public class DashState : BaseState
{
    float cooldown;
    public DashState(Enemy enemy) : base(enemy) { }

    public override void EnterState()
    {
        
    }

    public override void Execute()
    {
        if (!EnsurePlayer())
            return;
        if (_enemy.canDash)
        {
            _enemy.Dash();
        }
       
    }

    public override void ExitState() { }


    public override BaseState GetNextState()
    {  
        float dist = Vector3.Distance(
            _agent.transform.position,
            _player.transform.position
        );
        if(dist > (_enemy.GetAttackRange() / 2) || !_enemy.canDash)
        {
            return new GetInRangeState(_enemy);
        }
        return this;
    }
}