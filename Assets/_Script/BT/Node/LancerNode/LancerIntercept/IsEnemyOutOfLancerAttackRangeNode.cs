namespace _Script.BT.Node.LancerNode.LancerIntercept
{
    public class IsEnemyOutOfLancerAttackRangeNode: BTActionNode
    {
        private Lancer lancer;

        public IsEnemyOutOfLancerAttackRangeNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            return lancer.IsEnemyInAttackRange() ?  BTStatus.Failure : BTStatus.Success;
        }
    }
}