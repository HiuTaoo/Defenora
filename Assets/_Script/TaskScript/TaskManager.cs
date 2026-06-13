using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Task;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [Serializable]
    public class PendingTaskInfo
    {
        public Task task;
        public float releaseTime;

        public PendingTaskInfo(Task task, float releaseTime)
        {
            this.task = task;
            this.releaseTime = releaseTime;
        }
    }

    [Header("All Tasks (Global Blackboard)")] [SerializeField]
    private List<Task> allTasks = new();

    [Header("Pending/Unreachable Tasks")] [SerializeField]
    private List<PendingTaskInfo> pendingTasks = new();

    public IReadOnlyList<Task> AllTasks => allTasks;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (pendingTasks.Count > 0)
            for (var i = pendingTasks.Count - 1; i >= 0; i--)
                if (Time.time >= pendingTasks[i].releaseTime)
                {
                    var expiredTask = pendingTasks[i].task;
                    pendingTasks.RemoveAt(i);

                    if (expiredTask != null && !allTasks.Contains(expiredTask))
                    {
                        allTasks.Add(expiredTask);
                        Debug.Log($"[TaskManager] Đã khôi phục Task kẹt {expiredTask.taskType} về hàng đợi chính.");
                    }
                }
    }

    public void AddTask(Task task)
    {
        if (task == null)
            return;

        if (pendingTasks.Any(p => p.task == task))
            return;

        if (!allTasks.Contains(task))
        {
            allTasks.Add(task);
        }
    }

    public void RemoveTask(Task task)
    {
        if (task == null)
            return;

        if (allTasks.Remove(task))
        {
            Debug.Log($"[TaskManager] Remove task: {task.taskType}");
        }

        pendingTasks.RemoveAll(p => p.task == task);
    }
    
    public void MoveToPending(Task task, float cooldownDuration = 5f)
    {
        if (task == null) return;

        if (task.taskType == TaskType.TransportItem)
        {
            task.taskStatus = TaskStatus.Completed;
            RemoveTask(task);
            return;
        }

        allTasks.Remove(task);

        if (!pendingTasks.Any(p => p.task == task))
        {
            pendingTasks.Add(new PendingTaskInfo(task, Time.time + cooldownDuration));
        }
    }

    public IEnumerable<Task> GetAvailableTasks()
    {
        foreach (var task in allTasks)
        {
            if (!task.IsCompleted && task.HasFreeSlot())
                yield return task;
        }
    }

    public IEnumerable<Task> GetTasksByStatus(TaskStatus status)
    {
        foreach (var task in allTasks)
        {
            if (task.taskStatus == status)
                yield return task;
        }
    }
}