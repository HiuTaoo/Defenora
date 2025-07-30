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

    public Queue<Task> miniTasks = new Queue<Task>();
    public Task currentMiniTask;
    public Task parentTask;

    public bool isInPendingQueue = false;
    
    public Task(GameObject target, TaskType type, TaskStatus status = TaskStatus.NotStarted, int maxBuilders = 1, int layerIndex = 0)
    {
        targetGameObject = target;
        taskType = type;
        taskStatus = status;
        listBuilders = new List<Builder>();
        this.maxBuilders = maxBuilders;
        this.layerIndex = layerIndex;
    }

    public bool IsRootTask => parentTask == null;
    public bool IsCompleted => taskStatus == TaskStatus.Completed;

    public void AddMiniTask(Task miniTask)
    {
        miniTask.parentTask = this;
        miniTasks.Enqueue(miniTask);
    }

    public void TryAdvanceMiniTask()
    {
        if (miniTasks.Count > 0)
        {
            currentMiniTask = miniTasks.Dequeue();
        }
        else
        {
            currentMiniTask = null;
            taskStatus = TaskStatus.Completed;
        }
    }

    public bool HasUnfinishedMiniTasks()
    {
        return currentMiniTask != null || miniTasks.Count > 0;
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
