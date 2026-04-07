namespace _Script.BT.Node.LancerNode
{
    public class LancerStopMovingNode: BTActionNode
    {
        private Lancer lancer;

        public LancerStopMovingNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if(lancer.currentState == UnitState.Move)
                lancer.StopMove();
            return BTStatus.Success;
        }
    }
}