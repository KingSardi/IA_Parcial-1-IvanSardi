
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WolfPathFollower))]
public class WolfPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private List<NavigationNode> _patrolNodes = new();
    [SerializeField] private float _waitTime = 1f;
    [SerializeField] private float _retryTime = 2f;

    private WolfPathFollower _pathFollower;
    private int _currentNodeIndex;
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
            MoveToCurrentNode();
    }

    private void Update()
    {
        if (_isPaused || _patrolNodes.Count == 0)
            return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;

            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceNode();
                MoveToCurrentNode();
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
                MoveToCurrentNode();
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
        MoveToCurrentNode();
    }

    private void MoveToCurrentNode()
    {
        if (_patrolNodes.Count == 0)
            return;

        int attempts = 0;

        while (attempts < _patrolNodes.Count && _patrolNodes[_currentNodeIndex] == null)
        {
            AdvanceNode();
            attempts++;
        }

        if (_patrolNodes[_currentNodeIndex] == null)
            return;

        _pathFollower.SetDestination(_patrolNodes[_currentNodeIndex].Position);
        _retryTimer = _retryTime;
    }

    private void AdvanceNode()
    {
        _currentNodeIndex = (_currentNodeIndex + 1) % _patrolNodes.Count;
    }
}
