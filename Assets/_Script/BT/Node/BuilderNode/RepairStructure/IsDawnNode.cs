using UnityEngine;

namespace _Script.BT.Node.BuilderNode.RepairStructure
{
    public class IsDawnNode: BTActionNode
    {
        private Builder builder;

        public IsDawnNode(Unit unit) : base(unit)
        {
            builder = unit as  Builder;
        }

        public override BTStatus Tick()
        {
            if (TimeOfDaySystem.Instance.GetCurrentTime() is >= 6 and < 18)
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}