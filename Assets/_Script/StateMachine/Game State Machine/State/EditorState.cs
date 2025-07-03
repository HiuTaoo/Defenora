using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorState : IGameState
{
    private GameObject player;
    private MenuItem menuItem;
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Editor");

        context.UIManager.ShowUI(GameStateType.Editor);
        context.CameraManager.ApplyCameraSettings(GameStateType.Editor);
        DeSelectAllItem();
        UpdateCurrentLayerIndexUI();
        
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

    private void UpdateCurrentLayerIndexUI()
    {
        player = GetPlayer();
        int layer = player.GetComponentInChildren<FloorAgent>().currentFloorIndex;
        LayerManager.Instance.layerIndex = layer;
        BuildingGhostPreviewSystem.Instance.HandleLayerIndexChange(layer);
        LayerManager.Instance.MoveRibbonToLeft(layer);
        BuildingGhostPreviewSystem.Instance.HandleLayerIndexChange(layer);
    }

    private void DeSelectAllItem()
    {
        menuItem = GameObject.FindAnyObjectByType<MenuItem>();
        menuItem.DeSelectAllTileItem();
    }

    private GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }

}
