
using System.Collections.Generic;
using UnityEngine;

public class WolfPathFollower : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavigationGraph _navigationGraph;
    [SerializeField] private Transform _destination;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 8f;
    [SerializeField] private float _arrivalDistance = 0.3f;

    [Header("Movement Validation")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private LayerMask _restrictedLayer;
    [SerializeField] private float _agentRadius = 0.5f;

    private AStarPathfinder _pathfinder;
    private List<NavigationNode> _path = new();
    private int _currentNodeIndex;
    private bool _hasPath;
    private Vector3 _destinationPosition;
    private bool _hasDestination;

    public bool HasPath => _hasPath;
    public bool HasArrived { get; private set; }

    private void Awake()
    {
        _pathfinder = new AStarPathfinder(_restrictedLayer, _agentRadius);
    }

    private void Update()
    {
        if (!_hasPath)
            return;

        FollowPath();
    }

    public void SetDestination(Transform destination)
    {
        _destination = destination;
        _hasDestination = destination != null;
        _destinationPosition = _hasDestination ? destination.position : transform.position;
        HasArrived = false;
        CalculatePath();
    }

    public void SetDestination(Vector3 position)
    {
        _destination = null;
        _destinationPosition = position;
        _hasDestination = true;
        HasArrived = false;
        CalculatePath();
    }

    private void CalculatePath()
    {
        _hasPath = false;
        _path.Clear();
        _currentNodeIndex = 0;

        if (_navigationGraph == null || !_hasDestination)
            return;

        NavigationNode startNode = _navigationGraph.GetClosestNode(transform.position, true);
        NavigationNode goalNode = _navigationGraph.GetClosestNode(_destinationPosition, true);

        if (startNode == null || goalNode == null)
            return;

        _path = _pathfinder.FindPath(startNode, goalNode, isWolf: true);
        _hasPath = _path.Count > 0;
    }

    private void FollowPath()
    {
        if (_currentNodeIndex >= _path.Count)
        {
            Vector3 finalPosition = _destinationPosition;
            finalPosition.y = transform.position.y;

            if (Vector3.Distance(transform.position, finalPosition) <= _arrivalDistance)
            {
                _hasPath = false;
                HasArrived = true;
                return;
            }

            if (!CanMoveDirectlyTo(finalPosition))
            {
                _hasPath = false;
                return;
            }

            MoveTo(finalPosition);
            return;
        }

        Vector3 targetPosition = _path[_currentNodeIndex].Position;
        targetPosition.y = transform.position.y;

        if (!CanMoveDirectlyTo(targetPosition))
        {
            _hasPath = false;
            return;
        }

        MoveTo(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) <= _arrivalDistance)
            _currentNodeIndex++;
    }

    private bool CanMoveDirectlyTo(Vector3 targetPosition)
    {
        Vector3 origin = transform.position;
        Vector3 direction = targetPosition - origin;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (Physics.CheckSphere(origin, _agentRadius, _obstacleLayer))
            return false;

        if (Physics.CheckSphere(targetPosition, _agentRadius, _obstacleLayer))
            return false;

        if (Physics.CheckSphere(origin, _agentRadius, _restrictedLayer, QueryTriggerInteraction.Collide))
            return false;

        if (Physics.CheckSphere(targetPosition, _agentRadius, _restrictedLayer, QueryTriggerInteraction.Collide))
            return false;

        if (distance <= _arrivalDistance)
            return true;

        if (Physics.SphereCast(origin, _agentRadius, direction.normalized, out _, distance, _obstacleLayer))
            return false;

        if (Physics.SphereCast(origin, _agentRadius, direction.normalized, out _, distance, _restrictedLayer, QueryTriggerInteraction.Collide))
            return false;

        return true;
    }

    private void MoveTo(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}
