namespace _Script.BT.Node
{
    public abstract class BTActionNode : BTNode
    {
        protected Unit unit;

        protected BTActionNode(Unit unit)
        {
            this.unit = unit;
        }
    }
}