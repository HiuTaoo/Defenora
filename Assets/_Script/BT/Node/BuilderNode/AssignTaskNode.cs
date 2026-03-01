using System.Linq;
using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class AssignTaskNode : BTActionNode
    {
        public AssignTaskNode(Builder builder) : base(builder) {}

        public override BTStatus Tick()
        {
            if (builder.currentTask.targetGameObject == null)
            {
                Debug.Log("CurrentTask null");
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
                    Debug.Log("Can't find path");
                    return BTStatus.Failure;
                }
            }
            builder.targetGO = builder.currentTask.targetGameObject;
            return BTStatus.Success;
        }

    }
}