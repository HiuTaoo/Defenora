using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.TNTGoblinNode
{
    public class HasNPCInTNTGoblinAttackRangeNode: BTActionNode
    {
        private TNTGoblin tntGoblin;

        public HasNPCInTNTGoblinAttackRangeNode(Unit unit) : base(unit)
        {
            tntGoblin = unit as TNTGoblin;
        }
        
        public override BTStatus Tick()
        {
            if (tntGoblin == null)
            {
                return BTStatus.Failure;
            }
            
            var npcs = tntGoblin.DetectNPCs(tntGoblin.attackRange,
                tntGoblin.GetCurrentFacingVector());
            
            if (npcs.Count > 0)
            {
                tntGoblin.subTarget = tntGoblin.SelectClosestTarget(npcs);
                
                if (tntGoblin.subTarget != null)
                {
                    return BTStatus.Success;
                }
            }
            return BTStatus.Failure;
        }
    }
    
}