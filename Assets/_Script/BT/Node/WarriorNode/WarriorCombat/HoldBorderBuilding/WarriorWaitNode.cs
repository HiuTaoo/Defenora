using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class WarriorWaitNode : BTActionNode
    {
        private Warrior warrior;
        private float waitDuration = 5f;
        private float waitTimer = 0f;

        private float flipTimer = 0f;
        private float nextFlipTime;

        private bool isInitialized = false;

        public WarriorWaitNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (!isInitialized)
            {
                waitTimer = 0f;
                flipTimer = 0f;
                SetNextFlipTime();
                isInitialized = true;
                unit.animState = AnimState.Idle;
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
            
            if(warrior.aggroTimer < 0 && warrior.currentTarget == null)
                warrior.ClearAggro();
            
            return BTStatus.Running;
        }

        private void ResetNode()
        {
            isInitialized = false;
        }

        private void SetNextFlipTime()
        {
            nextFlipTime = Random.Range(1f, 3f);
        }
        
    }
}