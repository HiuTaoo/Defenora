using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorState : IGameState
{
    private readonly GameStateMachine gameStateMachine;
    private GameObject gameUI;

    public EditorState(GameStateMachine StateMachine, GameObject GUI)
    {
        this.gameStateMachine = StateMachine;
        this.gameUI = GUI;
    }

    public void Enter()
    {
        Debug.Log($"Game State: {gameStateMachine.CurrentState}");
        gameUI.SetActive( true );
    }

    public void Exit()
    {
        gameUI.SetActive( false );
    }

    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameStateMachine.ChangeState(GameStateType.Playing);
        }
    }

}
