
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private readonly Dictionary<NavigationNode, float> _gScore = new();
    private readonly Dictionary<NavigationNode, NavigationNode> _cameFrom = new();
    private readonly HashSet<NavigationNode> _closedSet = new();
    private readonly List<NavigationNode> _openSet = new();

    private readonly LayerMask _restrictedLayer;
    private readonly float _agentRadius;

    public AStarPathfinder(LayerMask restrictedLayer = default, float agentRadius = 0.5f)
    {
        _restrictedLayer = restrictedLayer;
        _agentRadius = agentRadius;
    }

    public List<NavigationNode> FindPath(NavigationNode start, NavigationNode goal, bool isWolf = false)
    {
        _gScore.Clear();
        _cameFrom.Clear();
        _closedSet.Clear();
        _openSet.Clear();

        if (start == null || goal == null)
            return new List<NavigationNode>();

        if (isWolf && (!start.WolvesAllowed || !goal.WolvesAllowed))
            return new List<NavigationNode>();

        _openSet.Add(start);
        _gScore[start] = 0f;

        while (_openSet.Count > 0)
        {
            NavigationNode current = GetLowestFCostNode(goal);

            if (current == goal)
                return ReconstructPath(current);

            _openSet.Remove(current);
            _closedSet.Add(current);

            foreach (NavigationNode neighbour in current.Neighbours)
            {
                if (neighbour == null || _closedSet.Contains(neighbour))
                    continue;

                if (isWolf && !neighbour.WolvesAllowed)
                    continue;

                if (!current.CanReachNeighbour(neighbour))
                    continue;

                if (isWolf && CrossesRestrictedZone(current.Position, neighbour.Position))
                    continue;

                float movementCost = Vector3.Distance(current.Position, neighbour.Position);
                float tentativeGScore = _gScore[current] + movementCost;

                if (!_gScore.TryGetValue(neighbour, out float previousGScore) || tentativeGScore < previousGScore)
                {
                    _cameFrom[neighbour] = current;
                    _gScore[neighbour] = tentativeGScore;

                    if (!_openSet.Contains(neighbour))
                        _openSet.Add(neighbour);
                }
            }
        }

        return new List<NavigationNode>();
    }

    private bool CrossesRestrictedZone(Vector3 start, Vector3 end)
    {
        if (_restrictedLayer.value == 0)
            return false;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (Physics.CheckSphere(start, _agentRadius, _restrictedLayer, QueryTriggerInteraction.Collide))
            return true;

        if (Physics.CheckSphere(end, _agentRadius, _restrictedLayer, QueryTriggerInteraction.Collide))
            return true;

        if (distance <= 0.001f)
            return false;

        return Physics.SphereCast(start, _agentRadius, direction.normalized, out _, distance, _restrictedLayer, QueryTriggerInteraction.Collide);
    }

    private NavigationNode GetLowestFCostNode(NavigationNode goal)
    {
        NavigationNode bestNode = null;
        float lowestFCost = float.MaxValue;

        foreach (NavigationNode node in _openSet)
        {
            float fCost = _gScore[node] + Heuristic(node, goal);

            if (fCost < lowestFCost)
            {
                lowestFCost = fCost;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private float Heuristic(NavigationNode current, NavigationNode goal)
    {
        return Vector3.Distance(current.Position, goal.Position);
    }

    private List<NavigationNode> ReconstructPath(NavigationNode current)
    {
        List<NavigationNode> path = new() { current };

        while (_cameFrom.TryGetValue(current, out NavigationNode previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
