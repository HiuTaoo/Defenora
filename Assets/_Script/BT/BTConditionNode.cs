namespace _Script.BT
{
    public abstract class BTConditionNode : BTNode
    {
        protected Builder builder;

        protected BTConditionNode(Builder builder)
        {
            this.builder = builder;
        }
    }
}