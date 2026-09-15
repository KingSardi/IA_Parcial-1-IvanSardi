using UnityEngine;

public class AttackState : State
{
    private FSMAgent _agent;

    public AttackState(FSMAgent agent, StateMachine stateMachine)
        : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        Debug.Log("Entró a Attack");
    }

    public override void Exit()
    {
        Debug.Log("Salió de Attack");
    }

    public override void Update()
    {
        if (_agent.CurrentTarget == null)
        {
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }
        
        float distance = Vector3.Distance(_agent.transform.position, _agent.CurrentTarget.transform.position);

        if (distance > _agent.PerceptionRadius)
        {
            _agent.CurrentTarget = null;
            StateMachine.ChangeState(HunterStates.Patrol);
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
        //_agent.CurrentAction = "ATAQUE CERCANO";
        _agent.ShowActionFeedback("CERCANO");
        Debug.Log($"Hunter realizó ataque MELEE a {_agent.CurrentTarget.name}");

        _agent.CurrentTarget.TakeDamage(_agent.MeleeDamage);

        _agent.ResetAttackTimer();
        _agent.CurrentTarget = null;

        StateMachine.ChangeState(HunterStates.Patrol);
    }

    private void RangeAttack()
    {
        //_agent.CurrentAction = "ATAQUE LEJANO";
        _agent.ShowActionFeedback("LEJANO");
        Debug.Log($"Hunter realizó ataque RANGED a {_agent.CurrentTarget.name}");

        _agent.CurrentTarget.TakeDamage(_agent.RangeDamage);

        _agent.ResetAttackTimer();
        _agent.CurrentTarget = null;

        StateMachine.ChangeState(HunterStates.Patrol);
    }

    private void Pursue()
    {
        _agent.CurrentAction = "PERSIGUIENDO";
        float predictionTime = 1f;

        Vector3 futurePosition =
            _agent.CurrentTarget.transform.position +
            _agent.CurrentTarget.Velocity * predictionTime;

        Vector3 direction =
            futurePosition - _agent.transform.position;

        direction.y = 0f;

        _agent.transform.position +=
            direction.normalized *
            _agent.speed *
            Time.deltaTime;
    }
}