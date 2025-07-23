using System.Collections;
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

    public Task(GameObject target, TaskType type, TaskStatus status = TaskStatus.NotStarted, int maxBuilders = 1, int layerIndex = 0)
    {
        targetGameObject = target;
        taskType = type;
        taskStatus = status;
        listBuilders = new List<Builder>();
        this.maxBuilders = maxBuilders;
        this.layerIndex = layerIndex;
    }

    public void AssignBuilder(Builder builder)
    {
        if (builder != null && !listBuilders.Contains(builder))
        {
            listBuilders.Add(builder);
            Debug.Log($"Builder {builder.name} assigned to task: {taskType} for target: {targetGameObject.name}");
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
    TransportItem
}

public enum TaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}
