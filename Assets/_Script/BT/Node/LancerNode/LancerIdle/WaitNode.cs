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

        public override BTStatus Tick()
        {
            if (!isInitialized)
            {
                waitTimer = 0f;
                flipTimer = 0f;
                SetNextFlipTime();
                isInitialized = true;
            }

            waitTimer += Time.deltaTime;
            flipTimer += Time.deltaTime;

            if (flipTimer >= nextFlipTime)
            {
                unit.UpdateFacing(-unit.transform.position);
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