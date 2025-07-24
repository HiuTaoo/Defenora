using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private UnitManager unitManager;

    public Queue<Task> newTaskQueue = new Queue<Task>();

    public List<Task> inProgressTask = new List<Task>();

    public Queue<Task> pendingTask = new Queue<Task>();

    public System.Action<Task> OnTaskCreated;

    public static TaskManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if(unitManager == null)
            GetUnitManager();
        
    }

    public void AddNewTask(Task task)
    {
        if (task == null) return;

        newTaskQueue.Enqueue(task);
        Debug.Log($"New task added: {task.taskType}");
        OnTaskCreated?.Invoke(task);
    }

    public void CompletedTask(Task task)
    {
        if (task == null) return;
        if(task.taskStatus == TaskStatus.Completed)
        {
            inProgressTask.Remove(task);
        }
            
    }

    private void GetUnitManager()
    {
        if(UnitManager.Instance != null)
            unitManager = UnitManager.Instance;
    }

}
