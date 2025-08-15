using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Task
{
    public GameObject targetGameObject;
    public TaskType taskType;
    public TaskStatus taskStatus;
    public int maxBuilders = 3;
    public int layerIndex;
    public List<Builder> listBuilders;

    [HideInInspector] public bool isInPendingQueue = false;

    public Task(GameObject target, TaskType type, TaskStatus status = TaskStatus.NotStarted, int maxBuilders = 1, int layerIndex = 0)
    {
        targetGameObject = target;
        taskType = type;
        taskStatus = status;
        listBuilders = new List<Builder>();
        this.maxBuilders = maxBuilders;
        this.layerIndex = layerIndex;
    }
    public bool IsCompleted => taskStatus == TaskStatus.Completed;

    public void CompleteTask(TaskType taskType)
    {
        taskStatus = TaskStatus.Completed;
        switch (taskType)
        {
            case TaskType.ChopTree:
                Debug.Log("Chopping tree completed.");
                break;
            case TaskType.BuildStructure:
                Debug.Log("Building structure completed.");
                break;
            case TaskType.MineResource:
                Debug.Log("Mining resource completed.");
                break;
            case TaskType.CraftItem:
                Debug.Log("Crafting item completed.");
                break;
            case TaskType.RepairStructure:
                Debug.Log("Repairing structure completed.");
                break;
            case TaskType.TransportItem:
                Debug.Log("Transporting item completed.");
                break;
            case TaskType.CleanUp:
                Debug.Log("Cleaning up completed.");
                break;
        }
    }
}

public enum TaskType
{
    ChopTree,
    BuildStructure,
    MineResource,
    CraftItem,
    RepairStructure,
    TransportItem, 
    CleanUp
}

public enum TaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}