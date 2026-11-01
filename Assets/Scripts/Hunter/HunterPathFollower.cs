
using System.Collections.Generic;
using UnityEngine;

public class HunterPathFollower : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavigationGraph _navigationGraph;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 8f;
    [SerializeField] private float _arrivalDistance = 0.3f;
    [SerializeField] private float _maxAcceleration = 12f;
    [SerializeField] private float _slowRadius = 1.5f;

    [Header("Movement Validation")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _agentRadius = 0.5f;

    private AStarPathfinder _pathfinder;
    private List<NavigationNode> _path = new();
    private int _currentNodeIndex;
    private bool _hasPath;
    private bool _directMovement;
    private Vector3 _destinationPosition;
    private Vector3 _velocity;

    public bool HasPath => _hasPath || _directMovement;
    public bool HasArrived { get; private set; }

    private void Awake()
    {
        _pathfinder = new AStarPathfinder();
    }

    private void Update()
    {
        if (_directMovement)
        {
            Vector3 targetPosition = _destinationPosition;
            targetPosition.y = transform.position.y;

            if (Vector3.Distance(transform.position, targetPosition) <= _arrivalDistance)
            {
                StopMovement(true);
                return;
            }

            if (!CanMoveDirectlyTo(targetPosition))
            {
                SetDestination(targetPosition);
                return;
            }

            MoveTo(targetPosition, true);
            return;
        }

        if (!_hasPath)
        {
            _velocity = Vector3.zero;
            return;
        }

        FollowPath();
    }

    public void SetDestination(Vector3 position)
    {
        _destinationPosition = position;
        HasArrived = false;

        if (TrySetDirectDestination(position))
            return;

        CalculatePath();
    }

    public void SetPath(List<NavigationNode> path, Vector3 destination)
    {
        if (path == null || path.Count == 0)
            return;

        int newStartIndex = 0;

        if (path.Count > 1 &&
            Vector3.Distance(transform.position, path[0].Position) <= _arrivalDistance)
        {
            newStartIndex = 1;
        }

        if (_hasPath && !_directMovement &&
            _currentNodeIndex < _path.Count &&
            newStartIndex < path.Count &&
            _path[_currentNodeIndex] == path[newStartIndex])
        {
            _destinationPosition = destination;
            return;
        }

        _destinationPosition = destination;
        _path = new List<NavigationNode>(path);
        _currentNodeIndex = newStartIndex;
        _directMovement = false;
        _hasPath = true;
        HasArrived = false;
    }

    public bool TrySetDirectDestination(Vector3 position)
    {
        Vector3 targetPosition = position;
        targetPosition.y = transform.position.y;

        if (!CanMoveDirectlyTo(targetPosition))
            return false;

        if (_directMovement)
        {
            _destinationPosition = targetPosition;
            HasArrived = false;
            return true;
        }

        _destinationPosition = targetPosition;
        _directMovement = true;
        _hasPath = false;
        _path.Clear();
        _currentNodeIndex = 0;
        HasArrived = false;

        return true;
    }

    public void Stop()
    {
        _hasPath = false;
        _directMovement = false;
        _velocity = Vector3.zero;
        _path.Clear();
        _currentNodeIndex = 0;
        HasArrived = false;
    }

    private void CalculatePath()
    {
        _directMovement = false;
        _hasPath = false;
        _path.Clear();
        _currentNodeIndex = 0;

        if (_navigationGraph == null)
            return;

        NavigationNode startNode = _navigationGraph.GetClosestNode(transform.position, false);
        NavigationNode goalNode = _navigationGraph.GetClosestNode(_destinationPosition, false);

        if (startNode == null || goalNode == null)
            return;

        _path = _pathfinder.FindPath(startNode, goalNode, isWolf: false);
        _hasPath = _path != null && _path.Count > 0;

        if (_hasPath && _path.Count > 1 && CanMoveDirectlyTo(_path[1].Position))
            _currentNodeIndex = 1;
    }

    private void FollowPath()
    {
        if (_currentNodeIndex >= _path.Count)
        {
            Vector3 finalPosition = _destinationPosition;
            finalPosition.y = transform.position.y;

            if (Vector3.Distance(transform.position, finalPosition) <= _arrivalDistance)
            {
                StopMovement(true);
                return;
            }

            if (!CanMoveDirectlyTo(finalPosition))
            {
                StopMovement(false);
                return;
            }

            MoveTo(finalPosition, true);
            return;
        }

        Vector3 targetPosition = _path[_currentNodeIndex].Position;
        targetPosition.y = transform.position.y;

        if (Vector3.Distance(transform.position, targetPosition) <= _arrivalDistance)
        {
            _currentNodeIndex++;
            return;
        }

        if (!CanMoveDirectlyTo(targetPosition))
        {
            StopMovement(false);
            return;
        }

        MoveTo(targetPosition, false);
    }

    private void MoveTo(Vector3 targetPosition, bool useArrive)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return;

        float targetSpeed = _moveSpeed;

        if (useArrive && distance < _slowRadius)
            targetSpeed *= distance / Mathf.Max(_slowRadius, 0.001f);

        Vector3 desiredVelocity = direction.normalized * targetSpeed;
        Vector3 steering = desiredVelocity - _velocity;

        steering = Vector3.ClampMagnitude(steering / Mathf.Max(Time.deltaTime, 0.0001f), _maxAcceleration);

        _velocity += steering * Time.deltaTime;
        _velocity = Vector3.ClampMagnitude(_velocity, _moveSpeed);
        _velocity.y = 0f;

        Vector3 displacement = _velocity * Time.deltaTime;

        if (displacement.sqrMagnitude > direction.sqrMagnitude)
            displacement = direction;

        Vector3 nextPosition = transform.position + displacement;

        if (!CanMoveDirectlyTo(nextPosition))
        {
            StopMovement(false);
            return;
        }

        transform.position = nextPosition;

        if (_velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    private void StopMovement(bool arrived)
    {
        _hasPath = false;
        _directMovement = false;
        _velocity = Vector3.zero;
        HasArrived = arrived;
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

        if (distance <= _arrivalDistance)
            return true;

        return !Physics.SphereCast(origin, _agentRadius, direction.normalized, out _, distance, _obstacleLayer);
    }
}
