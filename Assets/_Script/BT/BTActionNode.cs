namespace _Script.BT.Node
{
    public abstract class BTActionNode : BTNode
    {
        protected Builder builder;

        protected BTActionNode(Builder builder)
        {
            this.builder = builder;
        }
    }
}