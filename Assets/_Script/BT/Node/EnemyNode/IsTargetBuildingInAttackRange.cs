namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class IsTargetBuildingInAttackRange:BTActionNode
    {
        public IsTargetBuildingInAttackRange(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            if (unit.CheckTargetBuildingInAttackRange())
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}