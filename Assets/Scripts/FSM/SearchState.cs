
using UnityEngine;

public class SearchState : State
{
    private readonly FSMAgent _agent;
    private readonly PatrolData _data;

    private int _currentNode;
    private float _spawnTimer;
    private bool _isPlanting;
    private float _plantingTimer;

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
    }

    public override void Update()
    {
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

    private void PatrolLoop()
    {
        if (_data == null || _data.transform == null || _data.wayPoints == null || _data.wayPoints.Count == 0)
            return;

        Transform nextWaypoint = _data.wayPoints[_currentNode];

        if (nextWaypoint == null)
            return;

        Vector3 direction = nextWaypoint.position - _data.transform.position;
        direction.y = 0f;

        if (direction.magnitude <= _data.waypointCheckDistance)
        {
            _currentNode = (_currentNode + 1) % _data.wayPoints.Count;
            return;
        }

        _data.transform.position = Vector3.MoveTowards(
            _data.transform.position,
            nextWaypoint.position,
            _agent.speed * Time.deltaTime
        );
    }

    private void HandleInterestObjectSpawn()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _agent.InterestSpawnInterval)
            return;

        InterestObject[] activeObjects = Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None);

        _spawnTimer = 0f;

        if (activeObjects.Length >= 5)
            return;

        if (_agent.InterestObjectPrefab == null)
            return;

        _plantingTimer = 0f;
        _isPlanting = true;
        _agent.CurrentAction = "PLANTANDO";
    }

    private void HandlePlanting()
    {
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
