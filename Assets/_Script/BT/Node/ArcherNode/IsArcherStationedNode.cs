namespace _Script.BT.Node.ArcherNode
{
    public class IsArcherStationedNode : BTNode
    {
        private readonly Archer archer;

        public IsArcherStationedNode(Archer archer)
        {
            this.archer = archer;
        }

        public override BTStatus Tick()
        {
            return archer.isStationed ? BTStatus.Success : BTStatus.Failure;
        }
    }
}