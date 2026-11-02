
using System.Collections.Generic;
using UnityEngine;

public class SearchState : State
{
    private readonly FSMAgent _agent;
    private readonly NavigationGraph _graph;

    private NavigationNode _currentSearchNode;
    private float _spawnTimer;
    private bool _isPlanting;
    private float _plantingTimer;
    private bool _hasDestination;

    public SearchState(FSMAgent agent, NavigationGraph graph, StateMachine stateMachine)
        : base(stateMachine)
    {
        _agent = agent;
        _graph = graph;
    }

    public override void Enter()
    {
        _agent.CurrentAction = "BUSCANDO";
        _isPlanting = false;
        _plantingTimer = 0f;
        _hasDestination = false;
        _currentSearchNode = null;

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
        SearchLoop();
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

            _agent.PathFollower.SetDestination(destination, true);
            _hasDestination = _agent.PathFollower.HasPath;
        }
    }


    private void SearchLoop()
    {
        if (_graph == null || _graph.Nodes == null)
            return;

        if (_hasDestination && _agent.PathFollower.HasPath)
            return;

        List<NavigationNode> availableNodes = new();

        foreach (NavigationNode node in _graph.Nodes)
        {
            if (node == null)
                continue;

            if (!Bounds.Instance.Contains(node.Position))
                continue;

            if (node == _currentSearchNode)
                continue;

            availableNodes.Add(node);
        }

        if (availableNodes.Count == 0)
            return;

        NavigationNode nextNode =
            availableNodes[Random.Range(0, availableNodes.Count)];

        _currentSearchNode = nextNode;

        _agent.PathFollower.SetDestination(nextNode.Position, true);
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
