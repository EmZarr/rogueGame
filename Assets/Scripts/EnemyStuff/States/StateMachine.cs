using UnityEngine;
using UnityEngine.Rendering;

public class StateMachine : MonoBehaviour
{
    protected BaseState _currentState;
    protected bool _isTransitioning;
    [SerializeField] protected Enemy _enemy;


    void Start()
    {
        _isTransitioning = false;
        _currentState = new IdleState(_enemy);
        _currentState.EnterState();
    }

    // Update is called once per frame
    void Update()
    {
        //override for stuns
        if (_enemy.IsStunned && !(_currentState is StunnedState))
        {
            ChangeState(new StunnedState(_enemy));
            return;
        }

        BaseState nextState = _currentState.GetNextState();
        if (_currentState.Equals(nextState) && !_isTransitioning)
        {
            _currentState.Execute();
        }
        else if (!_isTransitioning)
        {
            ChangeState(nextState);
        }

    }

    void ChangeState(BaseState nextState)
    {
        _isTransitioning = true;
        _currentState.ExitState();
        _currentState = nextState;
        _currentState.EnterState();
        _isTransitioning = false;
    }

    public BaseState GetCurrentState()
    {
        return _currentState;
    }
}
