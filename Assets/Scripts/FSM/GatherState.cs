
using UnityEngine;

public class GatherState : State
{
    private readonly FSMAgent _agent;

    private float _gatherTimer;
    private float _repathTimer;
    private Vector3 _lastDestination;

    private const float RepathInterval = 0.5f;
    private const float RepathDistance = 1f;

    public GatherState(FSMAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _gatherTimer = 0f;
        _repathTimer = 0f;
        _lastDestination = Vector3.positiveInfinity;
        _agent.CurrentAction = "YENDO A RECOLECTAR";

        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    public override void Exit()
    {
        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    public override void Update()
    {
        if (_agent.HasCompletedGathers)
        {
            StateMachine.ChangeState(HunterStates.Exit);
            return;
        }

        if (!IsTargetValid())
        {
            ReturnToSearch();
            return;
        }

        Vector3 targetPosition = _agent.GatherTarget.transform.position;
        targetPosition.y = _agent.transform.position.y;

        float distance = Vector3.Distance(_agent.transform.position, targetPosition);

        if (distance > _agent.GatherDistance)
        {
            _gatherTimer = 0f;
            MoveToTarget(targetPosition);
            return;
        }

        if (_agent.PathFollower != null && _agent.PathFollower.HasPath)
            _agent.PathFollower.Stop();

        _agent.CurrentAction = "RECOLECTANDO";
        _gatherTimer += Time.deltaTime;

        if (_gatherTimer >= _agent.GatherDuration)
            Gather();
    }

    private bool IsTargetValid()
    {
        return _agent.GatherTarget != null &&
               _agent.GatherTarget.IsDead &&
               !_agent.GatherTarget.IsCollected;
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        _agent.CurrentAction = "YENDO A RECOLECTAR";

        if (_agent.PathFollower == null)
            return;

        _repathTimer -= Time.deltaTime;

        if (_repathTimer > 0f)
            return;

        if (_agent.PathFollower.HasPath &&
            Vector3.Distance(targetPosition, _lastDestination) < RepathDistance)
            return;

        _agent.PathFollower.SetDestination(targetPosition);
        _lastDestination = targetPosition;
        _repathTimer = RepathInterval;
    }

    private void Gather()
    {
        if (!IsTargetValid())
        {
            ReturnToSearch();
            return;
        }

        _agent.GatherTarget.Collect();
        _agent.RegisterGather();
        _agent.GatherTarget = null;

        _agent.CurrentAction = "RECOLECCION COMPLETA";

        if (_agent.HasCompletedGathers)
        {
            StateMachine.ChangeState(HunterStates.Exit);
            return;
        }

        StateMachine.ChangeState(HunterStates.Search);
    }

    private void ReturnToSearch()
    {
        _agent.GatherTarget = null;
        StateMachine.ChangeState(HunterStates.Search);
    }
}
