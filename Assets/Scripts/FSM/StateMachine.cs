using System;
using System.Collections.Generic;

public class StateMachine
{
    public State CurrentState { get; private set; }

    private readonly Dictionary<Enum, State> _states = new Dictionary<Enum, State>();

    public void RegisterState(Enum key, State state)
    {
        _states[key] = state;
    }

    public void ChangeState(Enum key)
    {
        if (!_states.TryGetValue(key, out State newState))
            return;

        if (newState == CurrentState)
            return;

        if (CurrentState is DeadState &&
    key.ToString() != HunterStates.Search.ToString())
        {
            return;
        }

        if (CurrentState is FleeState &&
    !key.Equals(HunterStates.Dead) &&
    !key.Equals(HunterStates.Search) &&
    !key.Equals(HunterStates.Exit))
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void EvaluateHunterThreat(bool wolfDetected, bool insideSafeZone)
    {
        if (CurrentState is DeadState)
            return;

        if (wolfDetected &&
            !insideSafeZone &&
            !(CurrentState is FleeState))
        {
            ChangeState(HunterStates.Flee);
        }
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}