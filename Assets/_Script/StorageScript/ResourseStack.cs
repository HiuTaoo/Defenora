namespace _Script.Resourse
{
    public class ResourseStack
    {
        public Storage.ResourceType resourceType;
        public int amount;

        public ResourseStack(Storage.ResourceType resourceType, int amount)
        {
            this.resourceType = resourceType;
            this.amount = amount;
        }
    }
}