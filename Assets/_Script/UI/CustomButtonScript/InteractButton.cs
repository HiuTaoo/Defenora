using System.Collections;
using System.Collections.Generic;
using _Script.Task;
using UnityEngine;

public class InteractButton : MonoBehaviour
{
    private Dictionary<InteractButtonState, GameObject> interactableText = new Dictionary<InteractButtonState, GameObject>();

    private void Awake()
    {
        RegisterButton();
        ChangeInteractButtonState(InteractButtonState.Collect);

        PlayerInteraction.Instance.OnInteractButtonPressed += HandleFButtonPressed;
    }

    #region Button Management
    private void RegisterButton()
    {
        var collectText = transform.Find("Text/Collect Text");
        var openText = transform.Find("Text/Open Text");
        var cutText = transform.Find("Text/Cut Text");
        var enterText = transform.Find("Text/Enter Text");

        interactableText.Add(InteractButtonState.Collect, collectText?.gameObject);
        interactableText.Add(InteractButtonState.Open, openText?.gameObject);
        interactableText.Add(InteractButtonState.Cut, cutText?.gameObject);
        interactableText.Add(InteractButtonState.Enter, enterText?.gameObject);
    }

    public void ChangeInteractButtonState(InteractButtonState state)
    {
        foreach (var text in interactableText.Values)
        {
            text.SetActive(false);
        }
        if (interactableText.ContainsKey(state))
        {
            interactableText[state].SetActive(true);
        }
    }
    #endregion

    #region Event Handling
    private void HandleFButtonPressed(GameObject obj, InteractButtonState state)
    {
        var treeComponent = obj.GetComponent<Tree>();
        var buildingComponent = obj.GetComponent<Building>();

        switch(state)
        {
            case InteractButtonState.Collect:

                break;
            case InteractButtonState.Open:

                break;
            case InteractButtonState.Cut:
                if (treeComponent != null)
                {
                    Debug.Log($"Cây: {treeComponent.name} đang trong hàng đợt. Đang tìm kiếm builder để chặt.");

                    var task = new Task(obj, TaskType.ChopTree,  1, treeComponent.layerIndex);
                    treeComponent.SetTask(task); 
                    
                    TaskManager.Instance.AddTask(task);
                }
                break;
            case InteractButtonState.Enter:
                if (buildingComponent != null)
                {
                    if (buildingComponent is TrainingBuilding trainingBuilding)
                    {
                        // Gọi UIManager bật cửa sổ Training lên màn hình
                        UIManager.Instance.ShowUI(GameStateType.Playing, UINames.TrainingWindow);
                        
                        // Tìm kiếm và ra lệnh cho Component UI bắt đầu nạp + làm mới dữ liệu lính của nhà này
                        TrainingWindowUI.Instance.OpenWindow(trainingBuilding);
                    }
                    Debug.Log($"Entering building: {buildingComponent.name}");
                }
                break;
        }
    }
    #endregion

}
public enum InteractButtonState
{
    Collect,
    Open,
    Cut,
    Enter
}