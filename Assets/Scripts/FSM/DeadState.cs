
using UnityEngine;

public class DeadState : State
{
    private readonly FSMAgent _hunter;
    private readonly float _respawnTime;
    private float _timer;

    public DeadState(FSMAgent hunter, StateMachine stateMachine, float respawnTime) : base(stateMachine)
    {
        _hunter = hunter;
        _respawnTime = respawnTime;
    }

    public override void Enter()
    {
        _timer = _respawnTime;
        _hunter.CurrentTarget = null;
        _hunter.GatherTarget = null;
        _hunter.CurrentAction = "DEAD";

        if (_hunter.PathFollower != null) 
            _hunter.PathFollower.Stop();
    }

    public override void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer > 0f)
            return;

        _hunter.Respawn();
        StateMachine.ChangeState(HunterStates.Search);
    }

    public override void Exit()
    {
        _hunter.CurrentAction = "NONE";
    }
}
