using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStateMachine
{
    private IUnitState currentState;

    public IUnitState CurrentState => currentState;

    public void ChangeState(IUnitState newState)
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

