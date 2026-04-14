using _Script.Task;

namespace _Script.BT.Node.BuilderNode.RepairStructure
{
    public class HasBrokenBuildingNode: BTActionNode
    {
        private Builder builder;

        public HasBrokenBuildingNode(Unit unit) : base(unit)
        {
            builder = unit as Builder;
        }

        public override BTStatus Tick()
        {
            Building buildingToRepair = builder.FindBestBuildingToRepair();

            if (buildingToRepair != null)
            {
                builder.currentTarget = buildingToRepair.transform;
                builder.currentTargetLayerIndex = buildingToRepair.layerIndex;

                var task = new global::Task(buildingToRepair.gameObject,
                    TaskType.RepairStructure, 
                    2,
                    buildingToRepair.layerIndex);
                
                TaskManager.Instance.AddTask(task);
                builder.currentTask = task;
                buildingToRepair.currentTask = task;
                return BTStatus.Success;
            }

            builder.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}