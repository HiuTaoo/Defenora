using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class AssignTaskNode : BTActionNode
    {
        private Builder builder;

        public AssignTaskNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask.targetGameObject == null)
            {
                Debug.Log("targetGameObject null");
                return BTStatus.Failure;
            }
            
            if (!builder.currentTask.Builders.Contains(builder))
            {
                if (!builder.currentTask.TryJoin(builder))
                {
                    Debug.Log("Can't join");
                    return BTStatus.Failure;
                }

                if (builder.currentTask.taskType == TaskType.TransportItem)
                {
                    builder.builderBlackBoard.pathFinding =
                        builder.FindBestPathToFront(builder.currentTask);
                }
                else
                    builder.builderBlackBoard.pathFinding =
                    builder.FindBestPathToAnyAdjacent(builder.currentTask);

                if (builder.builderBlackBoard.pathFinding == null)
                {
                    Debug.Log($"[AssignTask] Can't find path to task {builder.currentTask.taskType}. Blacklisting for 5s!");

                    TaskManager.Instance.MoveToPending(builder.currentTask);
                    
                    if (builder.currentTask.Builders.Contains(builder))
                    {
                        builder.currentTask.Leave(builder);
                    }

                    builder.currentTask = null;
                    builder.ResetState();

                    return BTStatus.Failure;
                }
            }
            builder.targetGO = builder.currentTask.targetGameObject;
            return BTStatus.Success;
        }

    }
}