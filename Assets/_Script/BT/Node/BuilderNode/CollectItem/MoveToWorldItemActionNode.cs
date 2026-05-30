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
                if (!FindAndMoveToNextItem()) return BTStatus.Failure;
                return BTStatus.Running;
            }

            if (builder.characterMovement.moving)
            {
                if (targetItem == null || !targetItem.gameObject.activeSelf)
                    if (!FindAndMoveToNextItem())
                    {
                        ClearState();
                        return BTStatus.Failure;
                    }

                return BTStatus.Running;
            }

            if (!FindAndMoveToNextItem())
            {
                hasStartedMove = false;
                targetItem = null;
                if (builder != null) builder.animState = AnimState.Idle;

                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        /// <summary>
        ///     Hàm nội bộ: Tìm kiếm vật phẩm gần nhất và ra lệnh di chuyển.
        /// </summary>
        private bool FindAndMoveToNextItem()
        {
            targetItem = ItemManager.Instance.FindNearestItem(
                builder.transform.position,
                builder.characterMovement.CurrentLayer, builder
            );
            if (targetItem == null) return false;

            targetItem.assignBuilder = builder;

            targetGridPos = builder.FindAdjacentWalkableCell(
                Vector3Int.FloorToInt(targetItem.transform.position),
                targetItem.layerIndex
            );

            builder.UpdateAnim();
            builder.animState = AnimState.Moving;
            builder.characterMovement.MoveToPosition(targetGridPos, targetItem.layerIndex);

            hasStartedMove = true;
            return true;
        }

        public override void ClearState()
        {
            base.ClearState();
            hasStartedMove = false;
            targetItem = null;
        }
    }
}