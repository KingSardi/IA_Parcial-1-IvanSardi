using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum HunterStates
{
    Patrol,
    Attack,
    Gather
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
    [SerializeField] private PatrolData dataPatrol;

    
    [Header("Interest Object Spawn")]
    [SerializeField] private InterestObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 5f;
    public float InterestSpawnInterval => _interestSpawnInterval;

    [Header("Attack")]
    [SerializeField] private float _meleeDamage = 3f;
    [SerializeField] private float _rangeDamage = 1f;

    public float MeleeDamage => _meleeDamage;
    public float RangeDamage => _rangeDamage;
    
    [Header("Visual Feedback")]
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private Renderer _hunterRenderer;

    
    public AdvanceAgent CurrentTarget { get; set; }
    public AdvanceAgent GatherTarget { get; set; }
    public string CurrentAction { get; set; } = "NONE";
    public InterestObject InterestObjectPrefab => _interestObjectPrefab;
    public bool CanAttack => _attackTimer <= 0f;
    public float PerceptionRadius => _perceptionRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float RangeAttackRadius => _rangeAttackRadius;

    private StateMachine _stateMachine;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null)
            {
                return "None";
            }

            return _stateMachine.CurrentState
                .GetType()
                .Name
                .Replace("State", "");
        }
    }

    public string LastAction { get; private set; } = "NONE";

    private float _lastActionTimer;
    private const float LastActionDuration = 1.5f;

    private void Awake()
    {
        _stateMachine = new StateMachine();

        //IdleState idleState = new IdleState(_stateMachine);
        PatrolState patrolState = new PatrolState(this, dataPatrol, _stateMachine);
        
        AttackState attackState = new AttackState(this, _stateMachine);
        GatherState gatherState = new GatherState(this, _stateMachine);

        //_stateMachine.RegisterState(PoliceStates.Idle, idleState);
        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);
        _stateMachine.RegisterState(HunterStates.Attack, attackState);
        _stateMachine.RegisterState(HunterStates.Gather, gatherState);

        _stateMachine.ChangeState(HunterStates.Patrol);
    }

    private void Update()
    {
        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
        }

        _stateMachine.Update();

        if (_stateText != null)
        {
            string targetName = "NONE";

            if (CurrentTarget != null)
            {
                targetName = CurrentTarget.name;
            }
            else if (GatherTarget != null)
            {
                targetName = GatherTarget.name;
            }

            if (_lastActionTimer > 0f)
            {
                _lastActionTimer -= Time.deltaTime;

                if (_lastActionTimer <= 0f)
                {
                    LastAction = "NONE";
                }
            }

            _stateText.text =
                $"State:{CurrentStateName.ToUpper()}\n" + $"Target: {targetName}\n" + $"Action: {CurrentAction}\n" + $"Last Attack: {LastAction}"; 
        }
        
        UpdateStateColor();
    }

    public void ResetAttackTimer()
    {
        _attackTimer = _TBA;
    }

    public AdvanceAgent FindClosestAliveBoid()
    {
    AdvanceAgent[] boids = Object.FindObjectsByType<AdvanceAgent>(FindObjectsSortMode.None);

    AdvanceAgent closestBoid = null; float closestDistance = Mathf.Infinity;

    foreach (AdvanceAgent boid in boids)
        {
        if (boid.IsDead)
            continue;

        float distance = Vector3.Distance(
            transform.position,
            boid.transform.position
        );

        if (distance <= _perceptionRadius &&
            distance<closestDistance)
            {
            closestDistance = distance;
            closestBoid = boid;
            }
        }

        return closestBoid;
    }
    public AdvanceAgent FindClosestDeadBoid()
    {
        AdvanceAgent[] boids =Object.FindObjectsByType<AdvanceAgent>(FindObjectsSortMode.None);

        AdvanceAgent closestBoid = null; float closestDistance = Mathf.Infinity;

        foreach (AdvanceAgent boid in boids)
        {
            if (!boid.IsDead || boid.IsCollected)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                boid.transform.position
            );

            if (distance <= _perceptionRadius &&
                distance < closestDistance)
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
            case "Patrol":
                _hunterRenderer.material.color = Color.green;
                break;

            case "Attack":
                _hunterRenderer.material.color = Color.red;
                break;

            case "Gather":
                _hunterRenderer.material.color = Color.yellow;
                break;
        }
    }
    public void ShowActionFeedback(string action)
    {
        LastAction = action;
        _lastActionTimer = LastActionDuration;
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