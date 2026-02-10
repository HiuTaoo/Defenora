using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class FindChopTaskNode : BTActionNode
    {
        public FindChopTaskNode(Builder builder) : base(builder) {}

        public override BTStatus Tick()
        {
            if (builder.currentTask != null)
            {
                if (builder.currentTask.targetGameObject != null &&
                    builder.currentTask.taskType == TaskType.ChopTree &&
                    !builder.currentTask.IsCompleted)
                {
                    return BTStatus.Success;
                }
            }

            var task = TaskManager.Instance
                .GetAvailableTasks()
                .FirstOrDefault(t => t.taskType == TaskType.ChopTree);

            if (task == null)
                return BTStatus.Failure;

            builder.currentTask = task;

            return BTStatus.Success;
        }
    }
}