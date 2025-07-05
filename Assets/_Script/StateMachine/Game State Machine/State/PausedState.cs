using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausedState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Paused");
        context.UIManager.HideAllUIs();
        context.UIManager.ShowUI(GameStateType.Paused, UINames.PauseMenu);
        // Pause game time
        Time.timeScale = 0f;

        // Có thể pause audio
        //context.AudioManager?.PlaySFX("pause_sound");
    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideUI(GameStateType.Paused, UINames.PauseMenu);
        // Resume game time
        Time.timeScale = 1f;
    }

    public void Tick(GameStateContext context)
    {
        if (context.InputManager.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.ChangeState(GameStateType.Playing);
        }
    }
}