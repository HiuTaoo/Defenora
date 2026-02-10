namespace _Script.Resourse
{
    public class ResourseStack
    {
        public ResourceType resourceType;
        public int amount;

        public ResourseStack(ResourceType resourceType, int amount)
        {
            this.resourceType = resourceType;
            this.amount = amount;
        }
    }
}