namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class LancerDefendNode: BTActionNode
    {
        private Lancer lancer;

        public LancerDefendNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (!lancer.CheckEnemyStillInRange(lancer.viewDistance))
                return BTStatus.Failure;
            
            lancer.currentState = UnitState.Defending;
            return BTStatus.Running;
        }
    }
}