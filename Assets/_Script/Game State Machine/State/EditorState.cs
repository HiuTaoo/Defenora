using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Editor");

        // Sử dụng các managers thông qua context
        context.UIManager.ShowUI(GameStateType.Editor);
        context.CameraManager.ApplyCameraSettings(GameStateType.Editor);
        //context.AudioManager?.PlayMusic("editor_music");
    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideUI(GameStateType.Editor);
    }

    public void Tick(GameStateContext context)
    {
        if (context.InputManager.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.ChangeState(GameStateType.Playing);
        }

        // Editor-specific logic có thể thêm ở đây
        HandleEditorInput(context);
    }

    private void HandleEditorInput(GameStateContext context)
    {
        // Example: Handle editor-specific controls
        if (context.InputManager.GetKey(KeyCode.LeftControl) &&
            context.InputManager.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Editor: Save triggered");
            // Save level logic
        }
    }
}
