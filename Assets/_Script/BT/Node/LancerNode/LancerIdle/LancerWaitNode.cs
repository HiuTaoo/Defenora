using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class LancerWaitNode : BTActionNode
    {
        private Lancer lancer;

        private float waitDuration = 10f;
        private float waitTimer = 0f;

        private float flipTimer = 0f;
        private float nextFlipTime;

        private bool isInitialized = false;

        public LancerWaitNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

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
                lancer.UpdateFacing(-lancer.transform.position);
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