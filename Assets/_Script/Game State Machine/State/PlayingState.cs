using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Playing");

        context.UIManager.ShowUI(GameStateType.Playing);
        context.CameraManager.ApplyCameraSettings(GameStateType.Playing);
        //context.AudioManager?.PlayMusic("gameplay_music");
    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideUI(GameStateType.Playing);
    }

    public void Tick(GameStateContext context)
    {
        if (context.InputManager.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.ChangeState(GameStateType.Paused);
        }

        if (context.InputManager.GetKeyUp(KeyCode.F7) )
        {
            if (context.InputManager.GetMovementInput() == Vector2.zero)
                context.StateMachine.ChangeState(GameStateType.Editor);
            else
                Debug.Log("Không thể mở editor trong trạng thái này!");
        }


        // Game logic có thể thêm ở đây
        HandleGameplayInput(context);
    }

    public void HandleGameplayInput(GameStateContext context)
    {
        // Example: Handle player movement, interactions, etc.
        Vector2 movement = context.InputManager.GetMovementInput();

    }
}
