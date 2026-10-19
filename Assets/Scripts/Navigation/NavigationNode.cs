
using System.Collections.Generic;
using UnityEngine;

public class NavigationNode : MonoBehaviour
{
    [Header("Node Connections")]
    [SerializeField]
    private List<NavigationNode> _neighbours = new();

    [Header("Connection Validation")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _agentRadius = 0.5f;
    public IReadOnlyList<NavigationNode> Neighbours => _neighbours;
    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Position, 0.35f);

        foreach (NavigationNode neighbour in _neighbours)
        {
            if (neighbour == null)
                continue;

            Vector3 direction = neighbour.Position - Position;
            float distance = direction.magnitude;

            if (distance < 0.01f)
                continue;

            bool blocked = Physics.SphereCast(
                Position,
                _agentRadius,
                direction.normalized,
                out _,
                distance,
                _obstacleLayer
            );

            Gizmos.color = blocked ? Color.red : Color.green;

            Gizmos.DrawLine(Position, neighbour.Position);
        }
    }
}
