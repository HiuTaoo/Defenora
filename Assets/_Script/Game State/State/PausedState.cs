using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausedState : IGameState
{
    private readonly GameStateMachine gameStateMachine;
    private GameObject gameUI;

    public PausedState(GameStateMachine StateMachine, GameObject GUI)
    {
        this.gameStateMachine = StateMachine;
        this.gameUI = GUI;
    }

    public void Enter()
    {
        Debug.Log($"Game State: {gameStateMachine.CurrentState}");
        gameUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Exit()
    {
        gameUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameStateMachine.ChangeState(GameStateType.Playing);
        }
    }
}
