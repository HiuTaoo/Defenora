namespace _Script.BT.Node.Public_Node
{
    public class IsNightTimeConditionNode : BTActionNode
    {
        public IsNightTimeConditionNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            if (TimeOfDaySystem.Instance != null && TimeOfDaySystem.Instance.IsNightTime()) return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}