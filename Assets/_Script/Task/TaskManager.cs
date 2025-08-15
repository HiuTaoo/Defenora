using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private UnitManager unitManager;

    public Queue<Task> newTaskQueue = new Queue<Task>();

    public Queue<Task> pendingTask = new Queue<Task>();

    public List<Task> listTaskInNewTaskQueue = new List<Task>();

    public List<Task> inProgressTask = new List<Task>();

    public List<Task> listTaskInPendingQueue = new List<Task>();

    public System.Action<Task> OnTaskCreated;

    public static TaskManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (unitManager == null)
            GetUnitManager();
        listTaskInPendingQueue = new List<Task>(pendingTask);
        listTaskInNewTaskQueue = new List<Task>(newTaskQueue);
    }

    public void AddNewTask(Task task)
    {
        if (task == null) return;

        newTaskQueue.Enqueue(task);
        Debug.Log($"New task has been added: {task.taskType}");
        OnTaskCreated?.Invoke(task);
    }

    public void CompletedTask(Task task)
    {
        if (task == null || !inProgressTask.Contains(task)) return;

        task.CompleteTask(task.taskType);
        inProgressTask.Remove(task);

        foreach(var builder in task.listBuilders)
        {
            if (builder != null)
            {
                builder.currentTask = null;
                builder.currentState = UnitState.Idle;
                builder.OnUnitIdle?.Invoke(builder);
            }
        }

    }

    private void GetUnitManager()
    {
        if (UnitManager.Instance != null)
            unitManager = UnitManager.Instance;
    }

}