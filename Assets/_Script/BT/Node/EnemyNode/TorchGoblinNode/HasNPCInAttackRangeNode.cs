using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class HasNPCInAttackRangeNode:BTActionNode
    {
        private TorchGoblin  torchGoblin;

        public HasNPCInAttackRangeNode(Unit unit) : base(unit)
        {
            torchGoblin = unit as TorchGoblin;
        }

        public override BTStatus Tick()
        {
            var npcs = torchGoblin.DetectNPCs(torchGoblin.attackRange,
                torchGoblin.GetCurrentFacingVector());
            if (npcs.Count > 0)
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}