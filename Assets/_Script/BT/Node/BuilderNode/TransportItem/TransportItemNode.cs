using _Script.BT.BlackBoard;
using _Script.Task;
using _Script.Unit_Management_System.Animation;
using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class TransportItemNode : BTActionNode
    {
        private Builder builder;
        private float delay = 0.6f;
        private float timer = 0f;
        private bool isDepositing = false;

        public TransportItemNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentTask == null || builder.currentTask.targetGameObject == null)
                return BTStatus.Failure;

            var storage = builder.currentTask.targetGameObject.GetComponent<Storage>();
            if (storage == null)
                return BTStatus.Failure;
            
            if (!isDepositing)
            {
                if (builder.currentInventory.IsEmpty)
                    return BTStatus.Failure;

                isDepositing = true;
                timer = 0f;
                builder.currentState = UnitState.Working;
                
                return BTStatus.Running;
            }
            
            timer += Time.deltaTime;

            if (timer < delay)
                return BTStatus.Running;

            if (builder.currentInventory.TryTakeOneStack(out var type, out var amount))
            {
                int added = storage.Add(type, amount);

                if (added > 0)
                {
                    builder.currentInventory.Remove(type, added);
                    builder.currentTask.taskStatus = TaskStatus.Completed;
                    TaskManager.Instance.RemoveTask(builder.currentTask);
                    builder.ResetState();
                }
            }
            
            isDepositing = false;
            timer = 0f;
            
            return BTStatus.Success;
        }
    }
}