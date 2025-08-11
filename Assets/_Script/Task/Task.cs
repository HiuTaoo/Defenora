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

    private readonly object miniTaskLock = new object();

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