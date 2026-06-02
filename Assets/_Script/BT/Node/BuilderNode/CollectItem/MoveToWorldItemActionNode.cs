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

                Vector3Int currentGridPos = Vector3Int.FloorToInt(builder.transform.position);
                if (currentGridPos == targetGridPos)
                {
                    ClearState();
                    return BTStatus.Success; 
                }

                return BTStatus.Running; 
            }

            return BTStatus.Running;
        }

        private bool EvaluateNextItem()
        {
            while (true)
            {
                targetItem = ItemManager.Instance.FindNearestItem(
                    builder.transform.position,
                    builder.floorAgent._currentFloorIndex, builder
                );

                if (targetItem == null)
                    return false;

                Vector3Int itemGridPos = Vector3Int.FloorToInt(targetItem.transform.position);
                targetGridPos = builder.FindAdjacentWalkableCell(itemGridPos, targetItem.layerIndex);

                if (!builder.CanCalculatePathToTarget(targetGridPos, targetItem.layerIndex))
                {
                    ItemManager.Instance.MoveToPending(targetItem);
                    continue; 
                }
                
                targetItem.assignBuilder = builder;
                return true;
            }
        }

        private void ExecuteMovementToTarget()
        {
            if (targetItem == null) return;

            builder.UpdateAnim();
            builder.animState = AnimState.Moving;
            
            // Phát lệnh dịch chuyển bộ
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