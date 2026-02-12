using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class StateMachine : MonoBehaviour
{
    protected BaseState _currentState;
    protected bool _isTransitioning;
    private bool _stunApplied;
    private bool _prevStopped;
    private MonoBehaviour _attackBehaviour;
    private Color _prevColor;
    private SpriteRenderer _sr;
    private NavMeshAgent _agent;
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
        if (_enemy.IsStunned)
        {
            ApplyStunOverlay();
            return;
        }

        RemoveStunOverlayIfNeeded();

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

    private void ApplyStunOverlay()
    {
        if (!_stunApplied)
        {
            _currentState.ExitState();
            _agent = _enemy.GetAgent();
            _attackBehaviour = _enemy.GetAttack() as MonoBehaviour;
            _sr = _enemy.GetComponentInChildren<SpriteRenderer>();

            if (_sr)
            {
                _prevColor = _sr.color;
                _sr.color = Color.cyan;
            }

            if (_agent && _agent.isActiveAndEnabled)
            {
                _prevStopped = _agent.isStopped;
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            if (_attackBehaviour)
                _attackBehaviour.enabled = false;

            _stunApplied = true;
        }
    }

    private void RemoveStunOverlayIfNeeded()
    {
        if (!_stunApplied) return;

        if (_sr) _sr.color = _prevColor;

        if (_agent && _agent.isActiveAndEnabled)
            _agent.isStopped = _prevStopped;

        if (_attackBehaviour)
            _attackBehaviour.enabled = true;

        _stunApplied = false;
    }

}
