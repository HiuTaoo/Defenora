using UnityEngine;

namespace _Script.BT.Node.BuilderNode
{
    public class WaitDurationNode : BTActionNode
    {
        private Builder builder;
        private float duration;
        private float startTime;
        private bool isWaiting = false;

        public WaitDurationNode(Unit unit, float waitDuration) : base(unit)
        {
            builder = (Builder)unit;
            duration = waitDuration;
        }

        public override BTStatus Tick()
        {
            if (!isWaiting)
            {
                startTime = Time.realtimeSinceStartup;
                isWaiting = true;
        
                if (builder.characterMovement != null)
                {
                    builder.characterMovement.StopMoving(); 
                }
        
                builder.currentState = UnitState.Idle;
                builder.animState = AnimState.Idle;
                builder.UpdateAnim();
                
                return BTStatus.Running;
            }

            if (Time.realtimeSinceStartup - startTime >= duration)
            {
                isWaiting = false;
                return BTStatus.Success; 
            }

            return BTStatus.Running;
        }
    }
}