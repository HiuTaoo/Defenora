using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ShootArrowNode : BTActionNode
    {
        private readonly Archer archer;
        private bool hasShot;

        private int lastFrameChecked = -1;
        private BTStatus lastStatus = BTStatus.Running;

        public ShootArrowNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            var target = archer.archerBlackBoard.detectedEnemy;

            if (target != null)
            {
                var targetHealth = target.GetComponentInChildren<Health>();

                if (!target.activeInHierarchy || (targetHealth != null && targetHealth.IsDead))
                {
                    Debug.Log(
                        $"[{archer.gameObject.name}] 🎯 Mục tiêu đã chết hoặc biến mất! Ngừng bắn mũi tên ngay lập tức.");

                    if (hasShot)
                    {
                        archer.EndAttackSignal();
                        archer.ResetAnim();
                        archer.currentState = UnitState.Idle;
                    }

                    archer.archerBlackBoard.detectedEnemy = null;

                    ResetInternal();
                    lastStatus = BTStatus.Failure;
                    return BTStatus.Failure;
                }
            }

            if (target == null && !hasShot)
            {
                ResetInternal();
                return BTStatus.Failure;
            }

            if (Time.frameCount == lastFrameChecked)
            {
                if (lastStatus == BTStatus.Running)
                {
                    ArcherFireDirection fireDir = archer.archerBlackBoard.fireDirection;
                    if (fireDir != ArcherFireDirection.None) archer.animFSM.SetFireDirection(fireDir);
                    
                    archer.animState = AnimState.Attacking;
                    archer.currentState = UnitState.Attack;
                }

                return lastStatus;
            }

            lastFrameChecked = Time.frameCount;

            if (hasShot && archer.isAttacking)
            {
                archer.currentState = UnitState.Attack;
                archer.animState = AnimState.Attacking;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            if (hasShot && !archer.isAttacking)
            {
                archer.nextFireTime = Time.time + archer.attackCooldown;

                archer.EndAttackSignal();
                archer.ResetAnim();
                archer.currentState = UnitState.Idle;

                ResetInternal();

                lastStatus = BTStatus.Success;
                return BTStatus.Success; 
            }

            if (!hasShot)
            {
                ArcherFireDirection fireDir = archer.archerBlackBoard.fireDirection;
                if (fireDir != ArcherFireDirection.None) archer.animFSM.SetFireDirection(fireDir);
                
                archer.StartAttackSignal();

                archer.currentState = UnitState.Attack;
                archer.animState = AnimState.Attacking;
                
                hasShot = true;
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            return BTStatus.Running;
        }

        public override void ClearState()
        {
            base.ClearState();
            if (archer != null && hasShot)
            {
                archer.EndAttackSignal();
                archer.ResetAnim();
                archer.currentState = UnitState.Idle;
            }

            ResetInternal();
        }

        private void ResetInternal()
        {
            hasShot = false;
            lastFrameChecked = -1;
            lastStatus = BTStatus.Running;
        }
    }
}