using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class CollectItemNode : BTActionNode
    {
        private float collectDelay = 1f;
        private float timer = 0f;
        private bool isCollecting = false;
        private Item targetItem;

        public CollectItemNode(Builder builder) : base(builder) { }

        public override BTStatus Tick()
        {
            if (!isCollecting)
            {
                targetItem = builder.FindItemAround();

                if (targetItem == null)
                    return BTStatus.Failure;

                isCollecting = true;
                timer = 0f;
            }

            timer += Time.deltaTime;

            if (timer < collectDelay)
            {
                return BTStatus.Running;
            }
            
            if (targetItem != null)
            {
                builder.PickupItem(targetItem);
            }

            isCollecting = false;
            targetItem = null;
            builder.UpdateAnim();
            builder.animFSM.ChangeState(UnitState.Idle);

            return BTStatus.Success;
        }
    }
}