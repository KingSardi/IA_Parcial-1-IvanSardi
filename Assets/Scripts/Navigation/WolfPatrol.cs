
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WolfPathFollower))]
public class WolfPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private List<Transform> _waypoints = new();
    [SerializeField] private float _waitTime = 1f;
    [SerializeField] private float _retryTime = 2f;

    private WolfPathFollower _pathFollower;
    private int _currentWaypointIndex;
    private float _waitTimer;
    private float _retryTimer;
    private bool _waiting;
    private bool _isPaused;

    private void Awake()
    {
        _pathFollower = GetComponent<WolfPathFollower>();
    }

    private void Start()
    {
        if (!_isPaused)
            MoveToCurrentWaypoint();
    }

    private void Update()
    {
        if (_isPaused || _waypoints.Count == 0)
            return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;

            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceWaypoint();
                MoveToCurrentWaypoint();
            }

            return;
        }

        if (_pathFollower.HasArrived)
        {
            _waiting = true;
            _waitTimer = _waitTime;
            return;
        }

        if (!_pathFollower.HasPath)
        {
            _retryTimer -= Time.deltaTime;

            if (_retryTimer <= 0f)
            {
                _retryTimer = _retryTime;
                MoveToCurrentWaypoint();
            }
        }
    }

    public void PausePatrol()
    {
        _isPaused = true;
        _waiting = false;
    }

    public void ResumePatrol()
    {
        _isPaused = false;
        _waiting = false;
        _retryTimer = _retryTime;
        MoveToCurrentWaypoint();
    }

    private void MoveToCurrentWaypoint()
    {
        if (_waypoints.Count == 0)
            return;

        int attempts = 0;

        while (attempts < _waypoints.Count && _waypoints[_currentWaypointIndex] == null)
        {
            AdvanceWaypoint();
            attempts++;
        }

        if (_waypoints[_currentWaypointIndex] == null)
            return;

        _pathFollower.SetDestination(_waypoints[_currentWaypointIndex]);
        _retryTimer = _retryTime;
    }

    private void AdvanceWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
    }
}
