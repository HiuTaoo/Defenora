using _Script.Task;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class CreateTransportTask : BTActionNode
    {
        private Builder builder;

        public CreateTransportTask(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentInventory == null || builder.currentInventory.IsEmpty)
                return BTStatus.Failure;

            var buildings = UnitManager.Instance.FindBuilding(BuildingType.Storage);
            if (buildings == null || buildings.Count == 0)
                return BTStatus.Failure;

            global::Storage bestStorage = null;
            float bestDistance = float.MaxValue;

            foreach (var building in buildings)
            {
                if(building.buildingState != BuildingState.Completed)
                    continue;
                
                if (!building.TryGetComponent(out global::Storage storage))
                    continue;

                if (storage.CurrentCapacity >= storage.maxStoreageCapacity)
                    continue;

                float dist = Vector3.SqrMagnitude(
                    builder.transform.position - storage.transform.position);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestStorage = storage;
                }
            }

            if (bestStorage == null)
                return BTStatus.Failure;

            if(builder.currentTask != null && builder.currentTask.taskType == TaskType.TransportItem)
                return BTStatus.Success;
            
            var task = new global::Task(
                bestStorage.gameObject,
                TaskType.TransportItem,
                1,
                bestStorage.LayerIndex);

            TaskManager.Instance.AddTask(task);
            builder.currentTask = task;

            return BTStatus.Success;
        }
    }
}