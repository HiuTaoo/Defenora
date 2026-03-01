using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class HasInterestObjectNode: BTActionNode
    {
        public HasInterestObjectNode(Builder builder): base(builder){}
        public override BTStatus Tick()
        {
            var interestObj = builder.FindInterestObject();
            if (interestObj == null)
            {
                Debug.Log("No interest object found");
                return BTStatus.Failure;
            }
            
            PathFinding path = builder.FindBestPathToAnyAdjacent(interestObj, builder.characterMovement.CurrentLayer);
            if (path == null)
            {
                Debug.Log("No path found");
                return BTStatus.Failure;
            }
                
            builder.targetDestination = interestObj.transform;
            builder.builderBlackBoard.pathFinding = path;
            Debug.Log("HasInterestObjectNode Tick");
            return BTStatus.Success;
        }
    }
}