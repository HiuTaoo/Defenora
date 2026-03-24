namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class LancerAttackNode: BTActionNode
    {
        private Lancer lancer;

        public LancerAttackNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (!lancer.IsEnemyInAttackRange())
                return BTStatus.Failure;
            
            lancer.currentState = UnitState.Attacking;
            return BTStatus.Running;
        }
    }
}