using System.Collections.Generic;
using UnityEngine;

public class PatrolState : State
{
    private FSMAgent _agent;
    private PatrolData _data;
    private int currentNode;
    private int direction = 1;
    private float _spawnTimer;
    private bool _isPlanting;
    private float _plantingTimer;

    public PatrolState(FSMAgent agent, PatrolData data, StateMachine stateMachine) :base(stateMachine)
    {
        _agent = agent;
        _data = data;
    }

    public override void Enter()
    {
        Debug.Log("Entre Patrol");
    }

    public override void Exit()
    {
        Debug.Log("Sali Patrol");
    }

    public override void Update()
    {

        if (_isPlanting)
        {
            HandlePlanting();
            return;
        }

        PatrolLoop();
        HandleInterestObjectSpawn();
        Debug.Log("Estoy en Patrol");
    }

    private void PatrolLoop()
    {
        var nextWaypoint = _data.wayPoints[currentNode];

        if (Vector3.Distance(nextWaypoint.position, _data.transform.position) <= _data.waypointCheckDistance)
        {
            currentNode = currentNode + 1 < _data.wayPoints.Count ? currentNode + 1 : 0;
        }

        var dir = nextWaypoint.position - _data.transform.position;
        _data.transform.position += dir.normalized * _agent.speed * Time.deltaTime;
    }

    private void HandleInterestObjectSpawn()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < _agent.InterestSpawnInterval)
            return;

        InterestObject[] activeObjects =
            Object.FindObjectsByType<InterestObject>(
                FindObjectsSortMode.None
            );

        if (activeObjects.Length >= 5)
        {
            _spawnTimer = 0f;
            return;
        }

        _spawnTimer = 0f;
        _plantingTimer = 0f;
        _isPlanting = true;

        Debug.Log("Hunter comenzó a plantar una trampa.");
    }

    private void HandlePlanting()
    {
        _plantingTimer += Time.deltaTime;

        if (_plantingTimer < 1f)
            return;

        Vector3 spawnPosition = _agent.transform.position;
        spawnPosition.y = 1f;

        Object.Instantiate(
            _agent.InterestObjectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        _isPlanting = false;

        Debug.Log("Hunter plantó una trampa.");
    }
}

[System.Serializable]
public class PatrolData
{
    public List<Transform> wayPoints;
    public Transform transform;
    public float waypointCheckDistance;
}