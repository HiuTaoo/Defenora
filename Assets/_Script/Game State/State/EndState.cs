using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndState : IGameState
{
    private readonly GameStateMachine gameStateMachine;
    private GameObject gameUI;

    public EndState(GameStateMachine StateMachine, GameObject GUI)
    {
        this.gameStateMachine = StateMachine;
        this.gameUI = GUI;
    }

    public void Enter()
    {
        Debug.Log($"Game State: {gameStateMachine.CurrentState}");
    }

    public void Exit()
    {

    }

    public void Tick()
    {

    }

}
