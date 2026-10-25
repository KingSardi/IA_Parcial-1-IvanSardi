using UnityEngine;

[RequireComponent(typeof(WolfPathFollower))]
[RequireComponent(typeof(WolfPatrol))]
[RequireComponent(typeof(WolfVisionSensor))]
[RequireComponent(typeof(WolfHowlEmitter))]
[RequireComponent(typeof(WolfAlertReceiver))]
public class WolfBrain : MonoBehaviour
{
    public enum WolfState
    {
        Patrol,
        Chase,
        Search
    }

    [Header("References")]
    [SerializeField] private Transform _hunter;

    [Header("Chase")]
    [SerializeField] private float _repathInterval = 0.5f;
    [SerializeField] private float _howlCooldown = 3f;

    [Header("Search")]
    [SerializeField] private float _searchDuration = 4f;

    private WolfPathFollower _pathFollower;
    private WolfPatrol _patrol;
    private WolfVisionSensor _vision;
    private WolfHowlEmitter _howl;
    private WolfAlertReceiver _alert;
    private SafeZoneDetector _safeZoneDetector;

    private WolfState _currentState = WolfState.Patrol;
    private Vector3 _lastKnownPosition;
    private float _repathTimer;
    private float _howlTimer;
    private float _searchTimer;

    public WolfState CurrentState => _currentState;

    private void Awake()
    {
        _pathFollower = GetComponent<WolfPathFollower>();
        _patrol = GetComponent<WolfPatrol>();
        _vision = GetComponent<WolfVisionSensor>();
        _howl = GetComponent<WolfHowlEmitter>();
        _alert = GetComponent<WolfAlertReceiver>();
        if (_hunter != null)
            _safeZoneDetector = _hunter.GetComponent<SafeZoneDetector>();
    }

    private void Update()
    {
        if (_hunter != null && _safeZoneDetector == null)
            _safeZoneDetector = _hunter.GetComponent<SafeZoneDetector>();

        if (_safeZoneDetector != null && _hunter != null && _safeZoneDetector.IsInsideSafeZone(_hunter.position))
        {
            _alert.ClearAlert();
            _lastKnownPosition = Vector3.zero;

            if (_currentState != WolfState.Patrol)
                ChangeState(WolfState.Patrol);

            return;
        }
        _repathTimer -= Time.deltaTime;
        _howlTimer -= Time.deltaTime;

        if (_vision.CanSeeHunter())
        {
            _lastKnownPosition = _vision.LastKnownHunterPosition;

            if (_currentState != WolfState.Chase)
                ChangeState(WolfState.Chase);

            UpdateChase();
            return;
        }

        if (_alert.HasAlert)
        {
            _lastKnownPosition = _alert.AlertPosition;
            _alert.ClearAlert();

            ChangeState(WolfState.Search);
        }
        else if (_currentState == WolfState.Chase)
        {
            ChangeState(WolfState.Search);
        }

        if (_currentState == WolfState.Search)
            UpdateSearch();

        if (_hunter.TryGetComponent<FSMAgent>(out var hunterAgent) && hunterAgent.IsDead)
        {
            _alert.ClearAlert();
            ChangeState(WolfState.Patrol);
            return;
        }
    }

    private void ChangeState(WolfState newState)
    {
        if (_currentState == newState)
            return;

        _currentState = newState;

        switch (_currentState)
        {
            case WolfState.Patrol:
                _patrol.ResumePatrol();
                break;

            case WolfState.Chase:
                _patrol.PausePatrol();
                _repathTimer = 0f;
                break;

            case WolfState.Search:
                _patrol.PausePatrol();
                _searchTimer = _searchDuration;
                _pathFollower.SetDestination(_lastKnownPosition);
                break;
        }
    }

    private void UpdateChase()
    {
        if (_hunter == null)
            return;

        if (_repathTimer <= 0f)
        {
            _pathFollower.SetDestination(_hunter.position);
            _repathTimer = _repathInterval;
        }

        if (_howlTimer <= 0f)
        {
            _howl.Howl(_hunter.position);
            _howlTimer = _howlCooldown;
        }
    }

    private void UpdateSearch()
    {
        if (_pathFollower.HasPath)
            return;

        if (!_pathFollower.HasArrived)
        {
            ChangeState(WolfState.Patrol);
            return;
        }

        _searchTimer -= Time.deltaTime;

        if (_searchTimer <= 0f)
            ChangeState(WolfState.Patrol);
    }
}