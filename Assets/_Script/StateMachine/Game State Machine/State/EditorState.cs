using UnityEngine;

public class EditorState : IGameState
{
    private GameObject player;
    private MenuItem menuItem;

    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: Editor");
        context.UIManager.HideAllUIs();
        context.UIManager.ShowUI(GameStateType.Editor, UINames.EditorMenu);
        context.CameraManager.ApplyCameraSettings(GameStateType.Editor);
        DeSelectAllItem();
        UpdateCurrentLayerIndexUI();
        
    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideUI(GameStateType.Editor, UINames.EditorMenu);
        if (EditBuildingManager.Instance != null)
            EditBuildingManager.Instance.ResetEditorManager();
        if (BuildingGhostPreviewSystem.Instance != null) BuildingGhostPreviewSystem.Instance.ClearAllGhostPreviews();
    }

    public void Tick(GameStateContext context)
    {
        switch (true)
        {
            case bool _ when context.InputManager.GetKeyDown(KeyCode.Escape):
                context.StateMachine.ChangeState(GameStateType.Playing);
                break;

            case bool _ when context.InputManager.GetKeyDown(KeyCode.Tab):
                if (LayerManager.Instance != null)
                    LayerManager.Instance.SwitchToNextLayer();
                break;
        }

        HandleEditorInput(context);
    }

    private void HandleEditorInput(GameStateContext context)
    {
        // Example: Handle editor-specific controls
        if (context.InputManager.GetKey(KeyCode.LeftControl) &&
            context.InputManager.GetKeyDown(KeyCode.S))
        {
            SaveLoadSystem.Instance.SaveGame();
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
