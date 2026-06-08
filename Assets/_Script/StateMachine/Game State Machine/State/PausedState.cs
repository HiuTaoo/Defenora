using UnityEngine;

public class PausedState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Paused");
        context.UIManager.HideAllUIs();
        context.UIManager.ShowUINotHideHistory(GameStateType.Paused, UINames.PauseMenu);
        context.UIManager.ShowUINotHideHistory(GameStateType.Paused, UINames.PauseButton);
        Time.timeScale = 0f;

        context.AudioManager.PauseMusic();
    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideAllUIs(GameStateType.Paused);
        Time.timeScale = 1f;
        context.AudioManager.ResumeMusic();
    }

    public void Tick(GameStateContext context)
    {
        if (context.InputManager.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.ChangeState(GameStateType.Playing);
        }
    }
}