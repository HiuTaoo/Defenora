using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ShootArrowNode : BTActionNode
    {
        private readonly float animDuration = 1f;
        private readonly Archer archer;
        private bool hasShot;

        private int lastFrameChecked = -1;
        private BTStatus lastStatus = BTStatus.Running;
        private float timer;

        public ShootArrowNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var target = archer.archerBlackBoard.detectedEnemy;

            if (target == null)
            {
                ResetInternal();
                return BTStatus.Failure;
            }

            if (Time.frameCount == lastFrameChecked)
            {
                if (lastStatus == BTStatus.Running)
                {
                    archer.animState = AnimState.Attacking;
                    archer.currentState = UnitState.Attack;
                }

                return lastStatus;
            }

            lastFrameChecked = Time.frameCount;

            if (!hasShot)
            {
                archer.animFSM.SetFireDirection(archer.archerBlackBoard.fireDirection);
                archer.currentState = UnitState.Attack;
                archer.animState = AnimState.Attacking;
                hasShot = true;
                timer = 0f;
            }

            archer.currentState = UnitState.Attack;
            archer.animState = AnimState.Attacking;

            timer += Time.deltaTime;

            if (timer >= animDuration)
            {
                archer.nextFireTime = Time.time + archer.attackCooldown;

                archer.ResetAnim();
                archer.currentState = UnitState.Idle;

                ResetInternal();

                lastStatus = BTStatus.Success;
                return BTStatus.Success;
            }

            lastStatus = BTStatus.Running;
            return BTStatus.Running;
        }

        public override void ClearState()
        {
            base.ClearState();
            if (archer != null && archer.animState == AnimState.Attacking)
            {
                archer.ResetAnim();
                archer.currentState = UnitState.Idle;
            }

            ResetInternal();
        }

        private void ResetInternal()
        {
            hasShot = false;
            timer = 0f;
            lastFrameChecked = -1;
            lastStatus = BTStatus.Running;
        }
    }
}