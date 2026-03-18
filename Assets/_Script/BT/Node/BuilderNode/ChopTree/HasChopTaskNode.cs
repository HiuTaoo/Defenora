using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class HasChopTaskNode : BTActionNode
    {
        private Builder builder;

        public HasChopTaskNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        
        public override BTStatus Tick() {
            if (builder.IsBusy)
                return BTStatus.Failure;
            
            if (builder.currentTask != null && builder.currentTask.targetGameObject 
                != null && builder.currentTask.taskType == TaskType.ChopTree) 
                return BTStatus.Success;
            
            bool hasGlobalTask = TaskManager.Instance.GetAvailableTasks()
                .Any(t => t.taskType == TaskType.ChopTree);
            return hasGlobalTask ? BTStatus.Success : BTStatus.Failure;
        }
    }
}