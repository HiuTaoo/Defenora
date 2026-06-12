using _Script.Unit_Management_System.Animation;
using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherDetectedEnemy
{
    public class ArcherRaidCombatActionNode : BTActionNode
    {
        private readonly Archer archer;
        private bool hasShot;

        private int lastFrameChecked = -1;
        private BTStatus lastStatus = BTStatus.Running;

        public ArcherRaidCombatActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (archer == null) return BTStatus.Failure;

            if (RaidManager.Instance != null && RaidManager.Instance.IsRaidActive)
                if (archer.currentTarget == null || archer.archerBlackBoard.detectedEnemy == null)
                {
                    var gate = RaidManager.Instance.activeRaidTarget;

                    archer.archerBlackBoard.detectedEnemy = gate;
                    archer.currentTarget = gate.transform;

                    var spawnPointComponent = gate.GetComponent<SpawnPoint>();
                    archer.currentTargetLayerIndex = spawnPointComponent != null ? spawnPointComponent.layerIndex : 0;

                    archer.aggroTimer = 9999f;
                    archer.isAlerted = true;
                }

            if (!hasShot)
            {
                if (Time.time < archer.nextFireTime)
                {
                    if (archer.characterMovement.moving) archer.StopMove();
                    archer.currentState = UnitState.Idle;
                    archer.animState = AnimState.Idle;
                    return BTStatus.Running;
                }

                var gate = RaidManager.Instance.activeRaidTarget;
                if (gate == null || !gate.activeInHierarchy)
                {
                    ResetInternal();
                    return BTStatus.Failure;
                }

                var facingDir = archer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                var enemiesInSight = archer.DetectEnemies(archer.attackRange, facingDir);
                var targetEnemy = archer.SelectClosestTarget(enemiesInSight);

                var finalTarget = targetEnemy != null ? targetEnemy : gate;

                var targetHealth = finalTarget.GetComponentInChildren<Health>();
                if (targetHealth != null && targetHealth.IsDead)
                {
                    ResetInternal();
                    return BTStatus.Running;
                }

                var currentDist = Vector2.Distance(archer.transform.position, finalTarget.transform.position);
                if (currentDist > archer.viewDistance)
                {
                    if (archer.characterMovement.moving) archer.StopMove();
                    archer.currentState = UnitState.Idle;
                    archer.animState = AnimState.Idle;
                    ResetInternal();
                    return BTStatus.Running;
                }

                Vector2 distance = finalTarget.transform.position - archer.transform.position;
                archer.archerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
                archer.UpdateFacing(archer.archerBlackBoard.lastDirection);

                archer.archerBlackBoard.detectedEnemy = finalTarget;
                archer.currentTarget = finalTarget.transform;

                var calculatedFireDir =
                    archer.GetFireDirection(archer.transform.position, finalTarget.transform.position);
                archer.archerBlackBoard.fireDirection = calculatedFireDir;
            }
            else
            {
                var target = archer.archerBlackBoard.detectedEnemy;
                if (target != null)
                {
                    var targetHealth = target.GetComponentInChildren<Health>();
                    if (!target.activeInHierarchy || (targetHealth != null && targetHealth.IsDead))
                    {
                        archer.EndAttackSignal();
                        archer.ResetAnim();
                        archer.currentState = UnitState.Idle;
                        archer.animState = AnimState.Idle;

                        archer.archerBlackBoard.detectedEnemy = null;
                        ResetInternal();
                        return BTStatus.Running;
                    }
                }
            }

            if (Time.frameCount == lastFrameChecked)
            {
                if (lastStatus == BTStatus.Running && hasShot)
                {
                    var fireDir = archer.archerBlackBoard.fireDirection;
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
                archer.animState = AnimState.Idle;

                ResetInternal();
                lastStatus = BTStatus.Running;
                return BTStatus.Running;
            }

            if (!hasShot)
            {
                if (archer.characterMovement.moving) archer.StopMove();

                var fireDir = archer.archerBlackBoard.fireDirection;
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
                archer.animState = AnimState.Idle;
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