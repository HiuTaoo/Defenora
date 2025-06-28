using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameStateMachine
{
    private readonly Dictionary<GameStateType, IGameState> states;
    private IGameState currentState;
    public IGameState CurrentState => currentState;

    public GameStateMachine()
    {
        states = new Dictionary<GameStateType, IGameState>();
    }

    public void RegisterState(GameStateType type, IGameState gameState) {
        states[type] = gameState;
    }

    public void ChangeState(GameStateType newStateType)
    {
        currentState?.Exit();
        currentState = states[newStateType];
        currentState.Enter();
    }

    public void Tick()
    {
        currentState?.Tick();
    }
}
