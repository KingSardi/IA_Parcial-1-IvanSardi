
using System.Collections.Generic;
using UnityEngine;

public class NavigationNode : MonoBehaviour
{
    [Header("Node Connections")]
    [SerializeField] private List<NavigationNode> _neighbours = new();
    
    [Header("Navigation Permissions")]
    [SerializeField] private bool _wolvesAllowed = true;

    public bool WolvesAllowed => _wolvesAllowed;

    [Header("Connection Validation")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _agentRadius = 0.5f;
    public IReadOnlyList<NavigationNode> Neighbours => _neighbours;
    public Vector3 Position => transform.position;


    public bool CanReachNeighbour(NavigationNode neighbour)
    {
        if (neighbour == null)
            return false;

        Vector3 direction = neighbour.Position - Position;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return true;

        if (Physics.CheckSphere(Position,_agentRadius,_obstacleLayer))
        {
            return false;
        }

        if (Physics.CheckSphere(neighbour.Position,_agentRadius,_obstacleLayer))
        {
            return false;
        }

        return !Physics.SphereCast(Position,_agentRadius,direction.normalized,out _,distance,_obstacleLayer );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Position, 0.35f);

        foreach (NavigationNode neighbour in _neighbours)
        {
            if (neighbour == null)
                continue;

            bool accessible = CanReachNeighbour(neighbour);

            Gizmos.color = accessible ? Color.green : Color.red;

            Gizmos.DrawLine(Position, neighbour.Position);
        }
    }
}
