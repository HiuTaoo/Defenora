using System.Collections.Generic;
using _Script.Task;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [Header("All Tasks (Global Blackboard)")]
    [SerializeField]
    private List<Task> allTasks = new List<Task>();

    public IReadOnlyList<Task> AllTasks => allTasks;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // =========================
    // TASK LIFECYCLE
    // =========================

    public void AddTask(Task task)
    {
        if (task == null)
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
    }

    // =========================
    // QUERY (OPTIONAL – RẤT HỮU ÍCH)
    // =========================

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

/*#if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.Label($"Tasks: {allTasks.Count}");
        foreach (var task in allTasks)
        {
            GUILayout.Label(
                $"{task.taskType} | {task.taskStatus} | {task.Builders.Count}/{task.maxBuilders}"
            );
        }
    }
#endif*/
}