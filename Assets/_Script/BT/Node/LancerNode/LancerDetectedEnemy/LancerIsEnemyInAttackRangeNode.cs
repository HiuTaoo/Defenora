namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class LancerIsEnemyInAttackRangeNode: BTActionNode
    {
        private Lancer lancer;

        public LancerIsEnemyInAttackRangeNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            return lancer.IsEnemyInAttackRange() ?  BTStatus.Success : BTStatus.Failure;
        }
    }
}