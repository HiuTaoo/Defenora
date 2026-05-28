using System.Collections.Generic;
using _Script.Task;
using UnityEngine;

[System.Serializable]
public class Task
{
    [Header("Core Info")]
    public string id; // <-- THÊM ID CHO TASK
    public GameObject targetGameObject;
    public TaskType taskType;
    public int layerIndex;

    [Header("Execution")]
    public TaskStatus taskStatus = TaskStatus.NotStarted;
    public int maxBuilders = 1;

    [Header("Progress (Optional)")]
    public float requiredProgress = 100f;
    public float currentProgress
    {
        get
        {
            if (targetGameObject == null) return 0f;

            if (taskType == TaskType.BuildStructure || taskType == TaskType.RepairStructure)
            {
                var building = targetGameObject.GetComponent<Building>();
                if (building != null)
                {
                    return building.currentBuildProgress; 
                }
            }

            // Bảo hiểm: Nếu bạn có thêm logic chặt cây cần đồng bộ
            if (taskType == TaskType.ChopTree)
            {
                var tree = targetGameObject.GetComponent<Tree>();
                if (tree != null) return tree.currentChopHit;
            }

            // Nếu không thuộc các loại trên, sử dụng một biến backing field chạy ngầm (nếu cần)
            return _internalProgress;
        }
        set
        {
            if (targetGameObject != null && (taskType == TaskType.BuildStructure || taskType == TaskType.RepairStructure))
            {
                var building = targetGameObject.GetComponent<Building>();
                if (building != null)
                {
                    building.currentBuildProgress = value;
                    return;
                }
            }
            _internalProgress = value;
        }
    }

    [SerializeField] private float _internalProgress = 0f;

    [SerializeField]
    private List<Builder> builders = new List<Builder>();
    public IReadOnlyList<Builder> Builders => builders;

    public bool IsCompleted => taskStatus == TaskStatus.Completed;

    public Task(
        GameObject target,
        TaskType type,
        int maxBuilders = 1,
        int layerIndex = 0)
    {
        this.id = System.Guid.NewGuid().ToString(); // <-- TỰ ĐỘNG TẠO ID DUY NHẤT
        targetGameObject = target;
        taskType = type;
        this.maxBuilders = maxBuilders;
        this.layerIndex = layerIndex;
        taskStatus = TaskStatus.NotStarted;
    }

    // Hàm bổ sung giúp set ID khi load từ file save
    public void SetId(string savedId)
    {
        this.id = savedId;
    }

    // =============================
    // SLOT MANAGEMENT
    // =============================

    public bool HasFreeSlot()
    {
        return builders.Count < maxBuilders;
    }

    public bool TryJoin(Builder builder)
    {
        if (builder == null || builders.Contains(builder))
            return false;

        if (!HasFreeSlot())
            return false;

        builders.Add(builder);
        taskStatus = TaskStatus.InProgress;
        return true;
    }

    public void Leave(Builder builder)
    {
        if (builders.Remove(builder))
        {
            if (builders.Count == 0 && taskStatus != TaskStatus.Completed)
                taskStatus = TaskStatus.NotStarted;
        }
    }

    // =============================
    // PROGRESS
    // =============================

    public void AddProgress(float amount)
    {
        if (taskStatus != TaskStatus.InProgress)
            return;

        currentProgress += amount;
        currentProgress = Mathf.Clamp(currentProgress, 0f, requiredProgress);

        if (currentProgress >= requiredProgress)
        {
            Complete();
        }
    }

    // =============================
    // COMPLETION
    // =============================

    public void Complete()
    {
        taskStatus = TaskStatus.Completed;
        builders.Clear();
    }
    
    public List<Builder> GetBuilders()
    {
        return builders;
    }
    
    public void ForceAssignBuilderOnLoad(Builder builder)
    {
        if (builder == null) return;
    
        if (!builders.Contains(builder))
        {
            builders.Add(builder);
        }
    }
}