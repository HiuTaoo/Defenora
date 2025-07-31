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
    public List<Task> listMiniTask = new List<Task>();

    public Task currentMiniTask = null;
    public Task parentTask = null;

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

    public bool IsRootTask => parentTask == null;
    public bool IsCompleted => taskStatus == TaskStatus.Completed;

    public void AddMiniTask(Task miniTask)
    {
        miniTask.parentTask = this;
        miniTasks.Enqueue(miniTask);
        listMiniTask.Add(miniTask);
    }

    public void TryAdvanceMiniTask()
    {
        lock (miniTasks) 
        {
            if (miniTasks.Count > 0)
            {
                currentMiniTask = miniTasks.Dequeue();
                listMiniTask.Remove(currentMiniTask);
            }
            else
            {
                currentMiniTask = null;
                taskStatus = TaskStatus.Completed;
            }
        }
    }

    public bool HasUnfinishedMiniTasks()
    {
        lock (miniTasks)
        {
            return currentMiniTask != null || miniTasks.Count > 0;
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