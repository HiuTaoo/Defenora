using System.Linq;
using _Script.Task;

namespace _Script.BT.Node.BuilderNode.Build
{
    public class FindBuildTaskNode :BTActionNode
    {
        private Builder builder;

        public FindBuildTaskNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask != null)
            {
                if(builder.currentTask.targetGameObject != null 
                   && !builder.currentTask.IsCompleted
                   && builder.currentTask.taskType == TaskType.BuildStructure)
                    return BTStatus.Success; 
            }
            
            var task = TaskManager.Instance.GetAvailableTasks()
                .FirstOrDefault(t => t.taskType == TaskType.BuildStructure && !builder.IsTaskBlacklisted(t));
            
            if(task == null)
                return BTStatus.Failure;
            
            builder.currentTask = task;
            return BTStatus.Success;
        }
    }
}