using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float _rangeAttackRadius = 8f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    public float speed = 3f;

    [Header("Perception")]
    [SerializeField] private float _perceptionRadius = 12f;
    [SerializeField] private PatrolData dataPatrol;

    
    [Header("Interest Object Spawn")]
    [SerializeField] private InterestObject _interestObjectPrefab;
    [SerializeField] private float _interestSpawnInterval = 5f;

    public InterestObject InterestObjectPrefab => _interestObjectPrefab;
    public float InterestSpawnInterval => _interestSpawnInterval;

    private StateMachine _stateMachine;

    private void Awake()
    {
        _stateMachine = new StateMachine();

        //IdleState idleState = new IdleState(_stateMachine);
        PatrolState patrolState = new PatrolState(this, dataPatrol, _stateMachine);

        //_stateMachine.RegisterState(PoliceStates.Idle, idleState);
        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);

        _stateMachine.ChangeState(HunterStates.Patrol);
    }

    private void Update()
    {
        _stateMachine.Update();
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