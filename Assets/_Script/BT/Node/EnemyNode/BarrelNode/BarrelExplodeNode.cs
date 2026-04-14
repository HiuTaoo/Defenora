using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.BarrelNode
{
    public class BarrelExplodeNode : BTActionNode
    {
        private Barrel barrel;

        public BarrelExplodeNode(Unit unit) : base(unit)
        {
            barrel = unit as Barrel;
        }

        public override BTStatus Tick()
        {
            if (barrel == null || barrel.currentState == UnitState.Dead)
                return BTStatus.Failure;

            barrel.Explosion(); 
            
            return BTStatus.Running; 
        }
    }
}