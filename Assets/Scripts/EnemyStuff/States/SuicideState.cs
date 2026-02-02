using UnityEngine;


public class SuicideState : BaseState
{
    public SuicideState(Enemy enemy) : base(enemy) { }

    public override void EnterState()
    {
        _agent.speed = 1f;
        var suiBomb = _enemy.gameObject.transform.GetComponentInChildren<DamageZone>();
        suiBomb.Activate(_enemy._data.damage, 0.1f, 5f, () => UnityEngine.GameObject.Destroy(_enemy.gameObject));
    }

    public override void Execute()
    {
        _agent.SetDestination(_player.transform.position);
    }

    public override void ExitState() { }

    public override BaseState GetNextState()
    {  
        return this;
    }
}