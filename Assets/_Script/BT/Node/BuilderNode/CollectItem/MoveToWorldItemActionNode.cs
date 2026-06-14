using _Script.ItemScript;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class MoveToWorldItemActionNode : BTActionNode
    {
        private readonly Builder builder;
        private bool hasStartedMove;
        private Item targetItem;
        private Vector3Int targetGridPos;

        public MoveToWorldItemActionNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentInventory != null && builder.currentInventory.IsFull)
            {
                ClearState();
                return BTStatus.Success;
            }

            if (!hasStartedMove)
            {
                if (!EvaluateNextItem()) 
                {
                    return BTStatus.Failure;
                }

                ExecuteMovementToTarget();
                return BTStatus.Running;
            }

            if (hasStartedMove)
            {
                if (targetItem == null || !targetItem.gameObject.activeSelf)
                {
                    if (!EvaluateNextItem())
                    {
                        ClearState();
                        return BTStatus.Failure;
                    }
                    ExecuteMovementToTarget();
                }

                var currentGridPos = GraphNode.Instance.WorldToGridPos(builder.transform.position,
                    builder.floorAgent._currentFloorIndex);
                
                if (currentGridPos == targetGridPos)
                {
                    ClearState();
                    return BTStatus.Success; 
                }

                if (!builder.characterMovement.moving)
                {
                    ClearState();
                    return BTStatus.Failure;
                }

                return BTStatus.Running; 
            }

            return BTStatus.Running;
        }

        private bool EvaluateNextItem()
        {
            var safetyCounter = 0;
            const int maxSafetyRetries = 10;

            while (safetyCounter < maxSafetyRetries)
            {
                safetyCounter++;

                targetItem = ItemManager.Instance.FindNearestItem(
                    builder.transform.position, builder
                );

                if (targetItem == null)
                    return false;

                var itemGridPos =
                    GraphNode.Instance.WorldToGridPos(targetItem.transform.position, targetItem.layerIndex);
                
                targetGridPos = builder.FindAdjacentWalkableCell(itemGridPos, targetItem.layerIndex);

                if (!builder.CanCalculatePathToTarget(targetGridPos, targetItem.layerIndex))
                {
                    ItemManager.Instance.MoveToPending(targetItem);
                    continue; 
                }
                
                targetItem.assignBuilder = builder;
                return true;
            }

            return false;
        }

        private void ExecuteMovementToTarget()
        {
            if (targetItem == null) return;

            builder.UpdateAnim();
            builder.animState = AnimState.Moving;
            
            builder.characterMovement.MoveToPosition(targetGridPos, targetItem.layerIndex);

            hasStartedMove = true;
        }

        public override void ClearState()
        {
            base.ClearState();
            hasStartedMove = false;
            targetItem = null;
        }
    }
}