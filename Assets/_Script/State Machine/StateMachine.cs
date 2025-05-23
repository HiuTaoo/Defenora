using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    private IState currentState;

    public IState CurrentState => currentState;

    public void ChangeState(IState newState)
    {
        if (newState == null || newState == currentState)
            return;

        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    public void Update() => currentState?.Update();
    public void FixedUpdate() => currentState?.FixedUpdate();
}

