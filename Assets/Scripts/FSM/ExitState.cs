
using UnityEngine;

public class ExitState : State
{
    private readonly FSMAgent _agent;
    private bool _hasDestination;
    private float _retryTimer;

    private const float RetryInterval = 0.5f;
    private const float ExitDistance = 0.8f;

    public ExitState(FSMAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.CurrentAction = "YENDO A LA SALIDA";
        _agent.CurrentTarget = null;
        _agent.GatherTarget = null;
        _hasDestination = false;
        _retryTimer = 0f;

        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    public override void Update()
    {
        if (_agent.ExitPoint == null || _agent.PathFollower == null)
            return;

        Vector3 destination = _agent.ExitPoint.position;
        destination.y = _agent.transform.position.y;

        if (Vector3.Distance(_agent.transform.position, destination) <= ExitDistance)
        {
            CompleteExit();
            return;
        }

        if (_hasDestination && _agent.PathFollower.HasPath)
            return;

        _retryTimer -= Time.deltaTime;

        if (_retryTimer > 0f)
            return;

        _agent.PathFollower.SetDestination(destination, true);
        _hasDestination = _agent.PathFollower.HasPath;
        _retryTimer = RetryInterval;
    }

    public override void Exit()
    {
        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    private void CompleteExit()
    {
        _agent.Respawn();
        StateMachine.ChangeState(HunterStates.Search);
    }
}
