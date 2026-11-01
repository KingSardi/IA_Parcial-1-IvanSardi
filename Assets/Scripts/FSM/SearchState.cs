
using System.Collections.Generic;
using UnityEngine;

public class SearchState : State
{
    private readonly FSMAgent _agent;
    private readonly PatrolData _data;

    private int _currentNode;
    private float _spawnTimer;
    private bool _isPlanting;
    private float _plantingTimer;
    private bool _hasDestination;

    public SearchState(FSMAgent agent, PatrolData data, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _data = data;
    }

    public override void Enter()
    {
        _agent.CurrentAction = "BUSCANDO";
        _isPlanting = false;
        _plantingTimer = 0f;
        _hasDestination = false;

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
        if (_agent.PathFollower == null)
            return;

        if (!IsInsideHuntingGround())
        {
            MoveToHuntingGround();
            return;
        }

        AdvanceAgent deadBoid = _agent.FindClosestDeadBoid();

        if (deadBoid != null)
        {
            _agent.GatherTarget = deadBoid;
            StateMachine.ChangeState(HunterStates.Gather);
            return;
        }

        AdvanceAgent target = _agent.FindClosestAliveBoid();

        if (_agent.CanAttack && target != null)
        {
            _agent.CurrentTarget = target;
            StateMachine.ChangeState(HunterStates.Attack);
            return;
        }

        if (_isPlanting)
        {
            HandlePlanting();
            return;
        }

        _agent.CurrentAction = "BUSCANDO";
        PatrolLoop();
        HandleInterestObjectSpawn();
    }

    private bool IsInsideHuntingGround()
    {
        return Bounds.Instance != null && Bounds.Instance.Contains(_agent.transform.position);
    }

    private void MoveToHuntingGround()
    {
        _agent.CurrentAction = "YENDO AL COTO";

        if (Bounds.Instance == null)
            return;

        if (!_hasDestination || _agent.PathFollower.HasArrived || !_agent.PathFollower.HasPath)
        {
            Vector3 destination = Bounds.Instance.transform.position;
            destination.y = _agent.transform.position.y;

            _agent.PathFollower.SetDestination(destination);
            _hasDestination = _agent.PathFollower.HasPath;
        }
    }

    private void PatrolLoop()
    {
        if (_data == null || _data.wayPoints == null || _data.wayPoints.Count == 0)
            return;

        if (_hasDestination && _agent.PathFollower.HasPath)
            return;

        if (_hasDestination && _agent.PathFollower.HasArrived)
            _currentNode = (_currentNode + 1) % _data.wayPoints.Count;

        Transform nextWaypoint = _data.wayPoints[_currentNode];

        if (nextWaypoint == null)
            return;

        _agent.PathFollower.SetDestination(nextWaypoint.position);
        _hasDestination = _agent.PathFollower.HasPath;
    }

    private void HandleInterestObjectSpawn()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _agent.InterestSpawnInterval)
            return;

        _spawnTimer = 0f;

        InterestObject[] activeObjects = Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None);

        if (activeObjects.Length >= 5 || _agent.InterestObjectPrefab == null)
            return;

        _plantingTimer = 0f;
        _isPlanting = true;
        _agent.CurrentAction = "PLANTANDO";
    }

    private void HandlePlanting()
    {
        _agent.CurrentAction = "PLANTANDO";
        _plantingTimer += Time.deltaTime;

        if (_plantingTimer < 1f)
            return;

        Vector3 spawnPosition = _agent.transform.position;
        spawnPosition.y = 1f;

        Object.Instantiate(_agent.InterestObjectPrefab, spawnPosition, Quaternion.identity);

        _isPlanting = false;
        _plantingTimer = 0f;
    }
}
