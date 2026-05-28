using System.Linq;
using _Script.Task;

namespace _Script.BT.Node.BuilderNode.RepairStructure
{
    public class HasRepairBuildingTaskNode: BTActionNode
    {
        private Builder builder;

        public HasRepairBuildingTaskNode(Unit unit) : base(unit)
        {
            builder = unit as Builder;
        }

        public override BTStatus Tick()
        {
            if (builder.IsBusy)
                return BTStatus.Failure;
            
            if (builder.currentTask != null && builder.currentTask.targetGameObject 
                != null && builder.currentTask.taskType == TaskType.RepairStructure) 
                return BTStatus.Success;
            
            bool hasGlobalTask = TaskManager.Instance.GetAvailableTasks()
                .Any(t => t.taskType == TaskType.RepairStructure);
            return hasGlobalTask ? BTStatus.Success : BTStatus.Failure;
        }
    }
}