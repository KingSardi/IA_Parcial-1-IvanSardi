
using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    [SerializeField] private List<NavigationNode> _nodes = new();
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _agentRadius = 0.5f;

    public IReadOnlyList<NavigationNode> Nodes => _nodes;

    public NavigationNode GetClosestNode(Vector3 position, bool isWolf = false)
    {
        NavigationNode closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (NavigationNode node in _nodes)
        {
            if (node == null)
                continue;

            if (isWolf && !node.WolvesAllowed)
                continue;

            Vector3 direction = node.Position - position;
            float distance = direction.magnitude;

            if (distance < 0.01f)
                return node;

            if (Physics.CheckSphere(position, _agentRadius, _obstacleLayer))
                continue;

            if (Physics.CheckSphere(node.Position, _agentRadius, _obstacleLayer))
                continue;

            if (Physics.SphereCast(position, _agentRadius, direction.normalized, out _, distance, _obstacleLayer))
                continue;

            float sqrDistance = direction.sqrMagnitude;

            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closestNode = node;
            }
        }

        return closestNode;
    }
}
