namespace _Script.Task
{
    public static class TaskPriority
    {
        public static int GetBasePriority(TaskType type)
        {
            switch (type)
            {
                case TaskType.RepairStructure: return 100;
                case TaskType.BuildStructure:  return 50;
                case TaskType.ChopTree:        return 75;
                case TaskType.TransportItem:       return 25;
                default: return 0;
            }
        }
    }

}