using UnityEngine;


public class SuicideState : BaseState
{
    private float timer;
    private bool isExploding;
    public SuicideState(Enemy enemy) : base(enemy) { }
    public override void EnterState()
    {
        timer = 0f;
        isExploding = false;

        _agent.isStopped = true;

        var suiBomb = _enemy.GetComponentInChildren<DamageZone>();

        // When enemy HP reaches 0 -> trigger explosion immediately
        _enemy.SetDeathEffect(() =>
        {
            suiBomb.Activate(_enemy._data.damage, 0.1f, 0f,
                () => UnityEngine.GameObject.Destroy(_enemy.gameObject));
        });

        // Normal suicide timer explosion
        suiBomb.Activate(_enemy._data.damage, 0.1f, 3f,
            () => UnityEngine.GameObject.Destroy(_enemy.gameObject));
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        if (timer > 1f && isExploding == false) {
            isExploding = true;
            _agent.isStopped = false;
            _agent.speed = _agent.speed * 2f;
        }
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            return;
        _agent.SetDestination(_player.transform.position);
    }

    public override void ExitState() { }

    public override BaseState GetNextState()
    {  
        return this;
    }
}