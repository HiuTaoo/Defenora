using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class IsInventoryFullNode: BTActionNode
    {
        private Builder builder;

        public IsInventoryFullNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            if (builder.currentInventory.IsFull)
            {
                return BTStatus.Success;
            }
          
            return BTStatus.Failure;
        }
    }
}