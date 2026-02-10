using System.Linq;
using _Script.Task;

namespace _Script.BT.Node.BuilderNode.Build
{
    public class HasBuildTaskNode :BTActionNode
    {
        public HasBuildTaskNode(Builder builder) : base(builder){}

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