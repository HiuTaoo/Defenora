namespace _Script.BT.Node.LancerNode.LancerDetectedEnemy.LancerCombatLoop
{
    public class IsEnemyInAttackRangeNode: BTActionNode
    {
        private Lancer lancer;

        public IsEnemyInAttackRangeNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            return lancer.IsEnemyInAttackRange() ?  BTStatus.Success : BTStatus.Failure;
        }
    }
}