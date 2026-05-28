using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class FindRepairTaskNode : BTActionNode
    {
        private Builder builder;

        public FindRepairTaskNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask != null)
            {
                if (builder.currentTask.targetGameObject != null &&
                    builder.currentTask.taskType == TaskType.RepairStructure &&
                    !builder.currentTask.IsCompleted)
                {
                    return BTStatus.Success;
                }
            }

            var task = TaskManager.Instance
                .GetAvailableTasks()
                .FirstOrDefault(t => t.taskType == TaskType.RepairStructure);

            if (task == null)
                return BTStatus.Failure;

            builder.currentTask = task;

            return BTStatus.Success;
        }
    }
}