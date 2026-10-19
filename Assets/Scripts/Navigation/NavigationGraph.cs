
using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    [Header("Graph Nodes")]
    [SerializeField]
    private List<NavigationNode> _nodes = new();

    [Header("Accessibility Settings")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _agentRadius = 0.5f;

    public IReadOnlyList<NavigationNode> Nodes => _nodes;

    public NavigationNode GetClosestNode(Vector3 position)
    {
        NavigationNode closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (NavigationNode node in _nodes)
        {
            if (node == null)
                continue;

            Vector3 direction = node.Position - position;
            float distance = direction.magnitude;

            if (distance < 0.01f)
                return node;

            bool startsOverlapping = Physics.CheckSphere(
                position,
                _agentRadius,
                _obstacleLayer
            );

            bool blocked = Physics.SphereCast(
                position,
                _agentRadius,
                direction.normalized,
                out _,
                distance,
                _obstacleLayer
            );

            if (startsOverlapping || blocked)
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
