using UnityEngine;

public class GatherState : State
{
    private FSMAgent _agent;

    private float _gatherDistance = 1.5f;
    private float _gatherDuration = 2f;
    private float _gatherTimer;

    public GatherState(FSMAgent agent, StateMachine stateMachine)
        : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _gatherTimer = 0f;
        Debug.Log($"Entró a Gather. Objetivo: {_agent.GatherTarget.name}");
    }

    public override void Exit()
    {
        Debug.Log("Salió de Gather");
    }

    public override void Update()
    {
        if (_agent.GatherTarget == null ||
            !_agent.GatherTarget.IsDead ||
            _agent.GatherTarget.IsCollected)
        {
            _agent.GatherTarget = null;
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        float distance = Vector3.Distance(
            _agent.transform.position,
            _agent.GatherTarget.transform.position
        );

        if (distance > _gatherDistance)
        {
            MoveToTarget();
            return;
        }

        _gatherTimer += Time.deltaTime;

        if (_gatherTimer >= _gatherDuration)
        {
            Gather();
        }
    }

    private void MoveToTarget()
    {
        _agent.CurrentAction = "RECOLECTANDO";
        Vector3 direction =
            _agent.GatherTarget.transform.position -
            _agent.transform.position;

        direction.y = 0f;

        _agent.transform.position +=
            direction.normalized *
            _agent.speed *
            Time.deltaTime;
    }

    private void Gather()
    {
        _agent.CurrentAction = "RECOLECTANDO";
        Debug.Log($"Hunter recolectó a {_agent.GatherTarget.name}");

        _agent.GatherTarget.Collect();

        _agent.GatherTarget = null;

        StateMachine.ChangeState(HunterStates.Patrol);
    }
}