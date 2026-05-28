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
            {
                ResetNodeInternal();
                return BTStatus.Failure;
            }

            var storage = builder.currentTask.targetGameObject.GetComponent<global::Storage>();
            if (storage == null)
            {
                RemoveCurrentTask();
                return BTStatus.Failure;
            }
            
            if (!isDepositing)
            {
                if (builder.currentInventory.IsEmpty)
                {
                    ResetNodeInternal();
                    return BTStatus.Failure;
                }

                isDepositing = true;
                timer = 0f;
                builder.currentState = UnitState.Working;
                builder.animState = AnimState.Working;
                builder.UpdateAnim();
                
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
                }
                else
                {
                    Debug.LogWarning($"[TransportItemNode] Kho {builder.currentTask.targetGameObject.name} đã đầy! Vẫn kết thúc Success để tìm kho mới.");
                }
            }
            
            RemoveCurrentTask();
            
            builder.currentState = UnitState.Idle;
            builder.animState = AnimState.Idle;
            builder.UpdateAnim();

            ResetNodeInternal();
            
            return BTStatus.Success; 
        }

        private void RemoveCurrentTask()
        {
            if (builder.currentTask != null)
            {
                builder.currentTask.taskStatus = TaskStatus.Completed;
                TaskManager.Instance.RemoveTask(builder.currentTask);
            }
            builder.ResetState(); 
        }

        private void ResetNodeInternal()
        {
            isDepositing = false;
            timer = 0f;
        }
    }
}