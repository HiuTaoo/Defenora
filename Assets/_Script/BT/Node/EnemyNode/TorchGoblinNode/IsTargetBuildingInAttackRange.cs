using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class IsTargetBuildingInAttackRange:BTActionNode
    {
        private TorchGoblin torchGoblin;

        public IsTargetBuildingInAttackRange(Unit unit) : base(unit)
        {
            torchGoblin = unit as  TorchGoblin;
        }

        public override BTStatus Tick()
        {
            if(torchGoblin.CheckTargetBuildingInAttackRange())
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}