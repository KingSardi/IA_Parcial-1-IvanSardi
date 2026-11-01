
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum HunterStates
{
    Search,
    Attack,
    Gather,
    Flee,
    Exit,
    Dead,
}

public class FSMAgent : MonoBehaviour
{
    [Header("Hunter Stats")]
    [SerializeField] private float _TBA = 3f;
    [SerializeField] private float _rangeAttackRadius = 5f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    public float speed = 3f;
    private float _attackTimer;

    [Header("Perception")]
    [SerializeField] private float _perceptionRadius = 12f;
    [SerializeField] private NavigationGraph _searchGraph;

    [Header("Interest Object Spawn")]
    [SerializeField] private InterestObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 5f;
    public float InterestSpawnInterval => _interestSpawnInterval;

    [Header("Attack")]
    [SerializeField] private float _meleeDamage = 3f;
    [SerializeField] private float _rangeDamage = 1f;

    public float MeleeDamage => _meleeDamage;
    public float RangeDamage => _rangeDamage;

    [Header("Gather")]
    [SerializeField] private int _requiredGathers = 3;
    [SerializeField] private float _gatherDistance = 1.5f;
    [SerializeField] private float _gatherDuration = 2f;

    private int _gatherCount;

    public int RequiredGathers => _requiredGathers;
    public int GatherCount => _gatherCount;
    public float GatherDistance => _gatherDistance;
    public float GatherDuration => _gatherDuration;
    public bool HasCompletedGathers => _gatherCount >= _requiredGathers;

    [Header("Exit")]
    [SerializeField] private Transform _exitPoint;

    public Transform ExitPoint => _exitPoint;

    [Header("Flee")]
    [SerializeField] private NavigationGraph _navigationGraph;
    [SerializeField] private SafeZoneDetector _safeZoneDetector;
    [SerializeField] private float _safeZoneWaitTime = 3f;

    private HunterWolfSensor _wolfSensor;

    public float SafeZoneWaitTime => _safeZoneWaitTime;


    [Header("Respawn")]
    [SerializeField] private Transform _respawnPoint;
    [SerializeField] private float _respawnTime = 5f;

   
    [Header("Visual Feedback")]
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private Renderer _hunterRenderer;

    [SerializeField]private Color _searchColor = new Color(0.52f, 0.80f, 0.09f);
    [SerializeField] private Color _patrolColor = Color.green;
    [SerializeField] private Color _attackColor = Color.red;
    [SerializeField] private Color _gatherColor = Color.yellow;
    [SerializeField] private Color _deadColor = Color.gray;
    [SerializeField] private Color _fleeColor = Color.cyan;
    [SerializeField] private Color _exitColor = Color.blue;

    public AdvanceAgent CurrentTarget { get; set; }
    public AdvanceAgent GatherTarget { get; set; }
    public string CurrentAction { get; set; } = "NONE";
    public InterestObject InterestObjectPrefab => _interestObjectPrefab;
    public bool CanAttack => _attackTimer <= 0f;
    public float PerceptionRadius => _perceptionRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public HunterPathFollower PathFollower => _pathFollower;

    private StateMachine _stateMachine;
    private HunterPathFollower _pathFollower;

    public bool IsDead => _stateMachine != null && _stateMachine.CurrentState is DeadState;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null)
                return "None";

            return _stateMachine.CurrentState.GetType().Name.Replace("State", "");
        }
    }

    public string LastAction { get; private set; } = "NONE";

    private float _lastActionTimer;
    private const float LastActionDuration = 1.5f;

    private void Awake()
    {
        _pathFollower = GetComponent<HunterPathFollower>();
        _stateMachine = new StateMachine();
        _wolfSensor = GetComponent<HunterWolfSensor>();

        SearchState searchState = new SearchState(this, _searchGraph, _stateMachine);
        AttackState attackState = new AttackState(this, _stateMachine);
        GatherState gatherState = new GatherState(this, _stateMachine);
        DeadState deadState = new DeadState(this, _stateMachine, _respawnTime);
        ExitState exitState = new ExitState(this, _stateMachine);
        FleeState fleeState = new FleeState(this, _navigationGraph, _safeZoneDetector, _stateMachine);

        _stateMachine.RegisterState(HunterStates.Search, searchState);
        _stateMachine.RegisterState(HunterStates.Attack, attackState);
        _stateMachine.RegisterState(HunterStates.Gather, gatherState);
        _stateMachine.RegisterState(HunterStates.Dead, deadState);
        _stateMachine.RegisterState(HunterStates.Exit, exitState);
        _stateMachine.RegisterState(HunterStates.Flee, fleeState);

        _stateMachine.ChangeState(HunterStates.Search);
    }

    private void Update()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;

        if (_wolfSensor != null && !IsDead)
        {
            _wolfSensor.Scan();

            bool insideSafeZone = _safeZoneDetector != null &&
                                  _safeZoneDetector.IsInsideSafeZone(transform.position);

            if (_wolfSensor.CanSeeWolf &&
                !insideSafeZone &&
                !(_stateMachine.CurrentState is FleeState))
            {
                _stateMachine.ChangeState(HunterStates.Flee);
            }
        }

        _stateMachine.Update();

        if (_lastActionTimer > 0f)
        {
            _lastActionTimer -= Time.deltaTime;

            if (_lastActionTimer <= 0f)
                LastAction = "NONE";
        }

        if (_stateText != null)
        {
            string targetName = "NONE";

            if (CurrentTarget != null)
                targetName = CurrentTarget.name;
            else if (GatherTarget != null)
                targetName = GatherTarget.name;

            _stateText.text =
                $"State: {CurrentStateName.ToUpper()}\n" +
                $"Target: {targetName}\n" +
                $"Action: {CurrentAction}\n" +
                $"Last Attack: {LastAction}\n" +
                $"Boids Collected: {GatherCount} / {RequiredGathers}";
        }

        UpdateStateColor();
    }

    public void ResetAttackTimer()
    {
        _attackTimer = _TBA;
    }

    public void Die()
    {
        if (IsDead || _stateMachine == null)
            return;

        _stateMachine.ChangeState(HunterStates.Dead);
    }

    public void Respawn()
    {
        if (_respawnPoint != null)
        {
            transform.position = _respawnPoint.position;
            transform.rotation = _respawnPoint.rotation;
        }

        CurrentTarget = null;
        GatherTarget = null;
        CurrentAction = "NONE";
        LastAction = "NONE";
        _lastActionTimer = 0f;
        _attackTimer = 0f;
        _gatherCount = 0;
    }

    public AdvanceAgent FindClosestAliveBoid()
    {
        AdvanceAgent[] boids = Object.FindObjectsByType<AdvanceAgent>(FindObjectsSortMode.None);

        AdvanceAgent closestBoid = null;
        float closestDistance = Mathf.Infinity;

        foreach (AdvanceAgent boid in boids)
        {
            if (boid.IsDead)
                continue;

            float distance = Vector3.Distance(transform.position, boid.transform.position);

            if (distance <= _perceptionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestBoid = boid;
            }
        }

        return closestBoid;
    }

    public AdvanceAgent FindClosestDeadBoid()
    {
        AdvanceAgent[] boids = Object.FindObjectsByType<AdvanceAgent>(FindObjectsSortMode.None);

        AdvanceAgent closestBoid = null;
        float closestDistance = Mathf.Infinity;

        foreach (AdvanceAgent boid in boids)
        {
            if (!boid.IsDead || boid.IsCollected)
                continue;

            float distance = Vector3.Distance(transform.position, boid.transform.position);

            if (distance <= _perceptionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestBoid = boid;
            }
        }

        return closestBoid;
    }

    private void UpdateStateColor()
    {
        if (_hunterRenderer == null)
            return;

        switch (CurrentStateName)
        {
            case "Search":
                _hunterRenderer.material.color = _searchColor;
                break;

            case "Attack":
                _hunterRenderer.material.color = _attackColor;
                break;

            case "Gather":
                _hunterRenderer.material.color = _gatherColor;
                break;

            case "Dead":
                _hunterRenderer.material.color = _deadColor;
                break;

            case "Flee":
                _hunterRenderer.material.color = _fleeColor;
                break;

            case "Exit":
                _hunterRenderer.material.color = _exitColor;
                break;
        }
    }

    public void ShowActionFeedback(string action)
    {
        LastAction = action;
        _lastActionTimer = LastActionDuration;
    }

    public void RegisterGather()
    {
        _gatherCount++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);
    }
}
