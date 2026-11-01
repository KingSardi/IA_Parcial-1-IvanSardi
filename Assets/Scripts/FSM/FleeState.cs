
using System.Collections.Generic;
using UnityEngine;

public class FleeState : State
{
    private readonly FSMAgent _agent;
    private readonly NavigationGraph _graph;
    private readonly SafeZoneDetector _safeZoneDetector;
    private readonly AStarPathfinder _pathfinder;

    private readonly List<Transform> _wolves = new();

    private Collider _selectedZone;
    private Vector3 _safeDestination;
    private float _waitTimer;
    private float _repathTimer;
    private bool _waiting;

    private const float RepathInterval = 2f;
    private const float DangerRadius = 7f;
    private const float CriticalDistance = 3f;
    private const float DangerPenalty = 100f;

    public FleeState(FSMAgent agent, NavigationGraph graph, SafeZoneDetector safeZoneDetector, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _graph = graph;
        _safeZoneDetector = safeZoneDetector;
        _pathfinder = new AStarPathfinder();
    }

    public override void Enter()
    {
        _agent.CurrentTarget = null;
        _agent.GatherTarget = null;
        _agent.CurrentAction = "ESCAPANDO";

        _selectedZone = null;
        _waitTimer = 0f;
        _repathTimer = 0f;
        _waiting = false;

        _agent.PathFollower?.Stop();

        UpdateWolves();
        SelectSafeZone();
        CalculateSafePath();
    }

    public override void Update()
    {
        if (_selectedZone == null || _agent.PathFollower == null)
            return;

        if (_safeZoneDetector != null &&
    _safeZoneDetector.IsInsideSafeZone(_agent.transform.position))
        {
            if (!_waiting)
            {
                _waiting = true;
                _waitTimer = 0f;
                _agent.PathFollower.Stop();
            }

            _agent.CurrentAction = "REFUGIADO";
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= _agent.SafeZoneWaitTime)
                StateMachine.ChangeState(_agent.HasCompletedGathers ? HunterStates.Exit : HunterStates.Search);

            return;
        }

        _agent.CurrentAction = "ESCAPANDO";

        if (_agent.PathFollower.HasPath)
            return;

        _repathTimer -= Time.deltaTime;

        if (_repathTimer > 0f)
            return;

        UpdateWolves();
        CalculateSafePath();
        _repathTimer = RepathInterval;
    }

    public override void Exit()
    {
        _agent.PathFollower?.Stop();
    }

    private void UpdateWolves()
    {
        _wolves.Clear();

        WolfBrain[] wolves = Object.FindObjectsByType<WolfBrain>(FindObjectsSortMode.None);

        foreach (WolfBrain wolf in wolves)
        {
            if (wolf != null && wolf.isActiveAndEnabled)
                _wolves.Add(wolf.transform);
        }
    }

    private float CalculateDangerCost(Vector3 start, Vector3 end)
    {
        float totalPenalty = 0f;

        Vector3 segment = end - start;
        segment.y = 0f;

        float segmentLengthSquared = segment.sqrMagnitude;

        foreach (Transform wolf in _wolves)
        {
            if (wolf == null)
                continue;

            Vector3 wolfPosition = wolf.position;
            wolfPosition.y = start.y;

            float t = 0f;

            if (segmentLengthSquared > 0.0001f)
            {
                Vector3 toWolf = wolfPosition - start;
                toWolf.y = 0f;

                t = Mathf.Clamp01(Vector3.Dot(toWolf, segment) / segmentLengthSquared);
            }

            Vector3 closestPoint = start + segment * t;
            float distance = Vector3.Distance(closestPoint, wolfPosition);

            if (distance >= DangerRadius)
                continue;

            if (distance <= CriticalDistance)
            {
                totalPenalty += DangerPenalty;
                continue;
            }

            float dangerFactor = 1f - (distance - CriticalDistance) / (DangerRadius - CriticalDistance);
            totalPenalty += DangerPenalty * dangerFactor;
        }

        return totalPenalty;
    }

    private void SelectSafeZone()
    {
        if (_graph == null || _safeZoneDetector == null || _safeZoneDetector.SafeZones == null)
            return;

        NavigationNode startNode = _graph.GetClosestNode(_agent.transform.position, false);

        if (startNode == null)
            return;

        float shortestDistance = float.PositiveInfinity;

        foreach (Collider zone in _safeZoneDetector.SafeZones)
        {
            if (zone == null || !zone.enabled)
                continue;

            Vector3 destination = zone.bounds.center;
            destination.y = _agent.transform.position.y;

            NavigationNode goalNode = _graph.GetClosestNode(destination, false);

            if (goalNode == null)
                continue;

            List<NavigationNode> path = _pathfinder.FindPath(startNode, goalNode, isWolf: false);

            if (path == null || path.Count == 0)
                continue;

            float distance = Vector3.Distance(_agent.transform.position, path[0].Position);

            for (int i = 1; i < path.Count; i++)
                distance += Vector3.Distance(path[i - 1].Position, path[i].Position);

            distance += Vector3.Distance(path[path.Count - 1].Position, destination);

            if (distance >= shortestDistance)
                continue;

            shortestDistance = distance;
            _selectedZone = zone;
            _safeDestination = destination;
        }
    }

    private void CalculateSafePath()
    {
        if (_graph == null || _selectedZone == null || _agent.PathFollower == null)
            return;

        NavigationNode startNode = _graph.GetClosestNode(_agent.transform.position, false);
        NavigationNode goalNode = _graph.GetClosestNode(_safeDestination, false);

        if (startNode == null || goalNode == null)
            return;

        List<NavigationNode> path = _pathfinder.FindPath(startNode, goalNode, isWolf: false, additionalCost: CalculateDangerCost);

        if (path == null || path.Count == 0)
            return;

        _agent.PathFollower.SetPath(path, _safeDestination);
    }

    private bool IsInsideSelectedZone()
    {
        Vector3 closestPoint = _selectedZone.ClosestPoint(_agent.transform.position);
        return (closestPoint - _agent.transform.position).sqrMagnitude <= 0.0001f;
    }
}
