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

    public Task parentTask = null;

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

    public bool IsRootTask => parentTask == null;
    public bool IsCompleted => taskStatus == TaskStatus.Completed;

    public void AddMiniTask(Task miniTask)
    {
        miniTask.parentTask = this;
        lock (miniTaskLock)
        {
            miniTasks.Enqueue(miniTask);
            listMiniTask.Add(miniTask);
        }
    }

    public Task TryGetNextMiniTask()
    {
        lock (miniTaskLock)
        {
            if (miniTasks.Count > 0)
            {
                Task nextTask = miniTasks.Dequeue();
                listMiniTask.Remove(nextTask);
                return nextTask;
            }
            return null;
        }
    }

    public bool HasUnfinishedMiniTasks()
    {
        lock (miniTaskLock)
        {
            return miniTasks.Count > 0;
        }
    }

    public bool AreAllMiniTasksCompleted()
    {
        lock (miniTaskLock)
        {
            return miniTasks.Count == 0;
        }
    }

    public bool HasNoMiniTasks()
    {
        lock (miniTaskLock)
        {
            return miniTasks.Count == 0 && listMiniTask.Count == 0;
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