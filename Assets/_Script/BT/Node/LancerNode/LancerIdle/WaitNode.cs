using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class WaitNode : BTActionNode
    {
        private float waitDuration = 10f;
        private float waitTimer = 0f;

        private float flipTimer = 0f;
        private float nextFlipTime;

        private bool isInitialized = false;

        public WaitNode(Unit unit) : base(unit)
        { }

        public WaitNode(Unit unit, float duration) : base(unit)
        {
            waitDuration = duration;
        }

        public override BTStatus Tick()
        {
            if (!isInitialized)
            {
                waitTimer = 0f;
                flipTimer = 0f;
                SetNextFlipTime();
                isInitialized = true;

                if (unit != null)
                {
                    unit.currentState = UnitState.Idle;
                    unit.animState = AnimState.Idle;
                }
            }

            waitTimer += Time.deltaTime;
            flipTimer += Time.deltaTime;

            if (flipTimer >= nextFlipTime)
            {
                if (unit != null)
                {
                    var currentXScaleDirection = unit.transform.localScale.x;

                    var reverseDirection = currentXScaleDirection > 0 ? Vector3.left : Vector3.right;

                    unit.UpdateFacing(reverseDirection);
                }

                flipTimer = 0f;
                SetNextFlipTime();
            }

            if (waitTimer >= waitDuration)
            {
                ResetNode();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private void ResetNode()
        {
            isInitialized = false;
        }

        private void SetNextFlipTime()
        {
            nextFlipTime = Random.Range(1f, 5f);
        }
    }
}