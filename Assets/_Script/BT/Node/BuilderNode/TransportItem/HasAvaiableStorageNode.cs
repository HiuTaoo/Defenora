using System.Linq;

namespace _Script.BT.Node.BuilderNode
{
    public class HasAvaiableStorageNode: BTActionNode
    {
        private Builder builder;

        public HasAvaiableStorageNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }
        public override BTStatus Tick()
        {
            var buildings = UnitManager.Instance.FindBuilding(BuildingType.Storage);
            var building = buildings.FirstOrDefault(b => b.currentCapacity < b.maxCapacity);

            if (building != null)
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}