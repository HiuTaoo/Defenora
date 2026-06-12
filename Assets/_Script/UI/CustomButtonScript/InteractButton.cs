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
        var attackText = transform.Find("Text/Attack Text");

        interactableText.Add(InteractButtonState.Collect, collectText?.gameObject);
        interactableText.Add(InteractButtonState.Open, openText?.gameObject);
        interactableText.Add(InteractButtonState.Cut, cutText?.gameObject);
        interactableText.Add(InteractButtonState.Enter, enterText?.gameObject);
        interactableText.Add(InteractButtonState.Attack, attackText?.gameObject);
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
                        UIManager.Instance.ShowUI(GameStateType.Playing, UINames.TrainingWindow);
                        
                        TrainingWindowUI.Instance.OpenWindow(trainingBuilding);
                    }
                    Debug.Log($"Entering building: {buildingComponent.name}");
                }
                break;
            case InteractButtonState.Attack:
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
    Enter,
    Attack
}