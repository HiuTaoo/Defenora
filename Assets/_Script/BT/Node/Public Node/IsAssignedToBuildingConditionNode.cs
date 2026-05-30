namespace _Script.BT.Node.Public_Node
{
    public class IsAssignedToBuildingConditionNode : BTActionNode
    {
        public IsAssignedToBuildingConditionNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            return unit.assignedBuilding != null ? BTStatus.Success : BTStatus.Failure;
        }
    }
}