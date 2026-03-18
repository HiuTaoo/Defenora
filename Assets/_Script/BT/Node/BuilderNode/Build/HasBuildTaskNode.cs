using System.Linq;
using _Script.Task;

namespace _Script.BT.Node.BuilderNode.Build
{
    public class HasBuildTaskNode :BTActionNode
    {
        private Builder builder;

        public HasBuildTaskNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask != null && builder.currentTask.targetGameObject != null
                                            && builder.currentTask.taskType == TaskType.BuildStructure)
                return BTStatus.Success;
            bool hasGlobalTask =
                TaskManager.Instance.GetAvailableTasks().Any(t => t.taskType == TaskType.BuildStructure);
            return hasGlobalTask ? BTStatus.Success : BTStatus.Failure;
        }
    }
}