
using UnityEngine;

public class AttackState : State
{
    private readonly FSMAgent _agent;

    private const float PredictionTime = 1f;
    private const float RepathInterval = 0.35f;
    private const float RepathDistance = 1.5f;

    private float _repathTimer;
    private Vector3 _lastDestination;

    public AttackState(FSMAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.CurrentAction = "PERSIGUIENDO";
        _repathTimer = 0f;
        _lastDestination = Vector3.positiveInfinity;

        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    public override void Exit()
    {
        if (_agent.PathFollower != null)
            _agent.PathFollower.Stop();
    }

    public override void Update()
    {
        if (_agent.CurrentTarget == null || _agent.CurrentTarget.IsDead)
        {
            ReturnToSearch();
            return;
        }

        float distance = Vector3.Distance(_agent.transform.position, _agent.CurrentTarget.transform.position);

        if (distance > _agent.PerceptionRadius)
        {
            ReturnToSearch();
            return;
        }

        if (distance <= _agent.MeleeAttackRadius)
        {
            MeleeAttack();
            return;
        }

        if (distance <= _agent.RangeAttackRadius)
        {
            RangeAttack();
            return;
        }

        Pursue();
    }

    private void MeleeAttack()
    {
        _agent.CurrentAction = "ATAQUE CERCANO";
        _agent.ShowActionFeedback("CERCANO");

        _agent.CurrentTarget.TakeDamage(_agent.MeleeDamage);
        _agent.ResetAttackTimer();

        ReturnToSearch();
    }

    private void RangeAttack()
    {
        _agent.CurrentAction = "ATAQUE LEJANO";
        _agent.ShowActionFeedback("LEJANO");

        _agent.CurrentTarget.TakeDamage(_agent.RangeDamage);
        _agent.ResetAttackTimer();

        ReturnToSearch();
    }

    private void Pursue()
    {
        _agent.CurrentAction = "PERSIGUIENDO";

        if (_agent.PathFollower == null)
            return;

        Vector3 futurePosition = _agent.CurrentTarget.transform.position + _agent.CurrentTarget.Velocity * PredictionTime;
        futurePosition.y = _agent.transform.position.y;

        _repathTimer -= Time.deltaTime;

        if (_repathTimer > 0f)
            return;

        if (Vector3.Distance(futurePosition, _lastDestination) < RepathDistance && _agent.PathFollower.HasPath)
            return;

        _agent.PathFollower.SetDestination(futurePosition);

        _lastDestination = futurePosition;
        _repathTimer = RepathInterval;
    }

    private void ReturnToSearch()
    {
        _agent.CurrentTarget = null;
        StateMachine.ChangeState(HunterStates.Search);
    }
}
