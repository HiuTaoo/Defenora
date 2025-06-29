using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameStateMachine
{
    private readonly Dictionary<GameStateType, IGameState> states;
    private IGameState currentState;
    private GameStateContext context;

    public IGameState CurrentState => currentState;
    public GameStateType CurrentStateType { get; private set; }

    public GameStateMachine()
    {
        states = new Dictionary<GameStateType, IGameState>();
        context = new GameStateContext(this);
    }

    public void SetContext(GameStateContext gameContext)
    {
        context = gameContext;
        context.StateMachine = this;
    }

    public void RegisterState(GameStateType type, IGameState gameState)
    {
        states[type] = gameState;
    }

    public void ChangeState(GameStateType newStateType)
    {
        var previousState = CurrentStateType;

        currentState?.Exit(context);
        currentState = states[newStateType];
        CurrentStateType = newStateType;
        currentState.Enter(context);

        Debug.Log($"State changed: {previousState} → {newStateType}");
    }

    public void Tick()
    {
        currentState?.Tick(context);
    }
}
