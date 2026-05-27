using _Script.Unit_Management_System.Animation;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class HasItemInInventoryNode : BTActionNode
    {
        private Builder builder;

        public HasItemInInventoryNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentInventory == null
                || builder.currentInventory.IsEmpty)
                return BTStatus.Failure;

            // Hàm TryGetMostAbundant bây giờ sẽ trả ra 'ItemData' thông qua biến 'resource'
            if (builder.currentInventory.TryGetMostAbundant(out var resource))
            {
                // builder.currentResource lúc này đã là kiểu ItemData nên gán trực tiếp mượt mà
                builder.currentResource = resource.resourceType;
                builder.currentTool = ToolType.None;
                builder.UpdateAnim();
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }
}