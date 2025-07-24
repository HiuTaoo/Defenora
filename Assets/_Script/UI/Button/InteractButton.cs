using System.Collections;
using System.Collections.Generic;
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
                    Debug.Log($"Tree: {treeComponent.name} is in queue. Looking for nearest builder to cut.");

                    var task = new Task(obj, TaskType.ChopTree, TaskStatus.NotStarted, 3, treeComponent.layerIndex);
                    treeComponent.currentTask = task;
                    TaskManager.Instance.AddNewTask(task);
                }
                break;
            case InteractButtonState.Enter:
                if (buildingComponent != null)
                {
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