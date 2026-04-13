using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode
{
    public class MoveToTargetBuildingNode : BTActionNode
    {
        private TorchGoblin goblin;
        private bool hasStartedMove = false;
        private bool isAligningTarget = false;

        public MoveToTargetBuildingNode(Unit unit) : base(unit)
        {
            goblin = (TorchGoblin)unit;
        }

        public override BTStatus Tick()
        {
            if (goblin.isKnockedBack)
            {
                ResetNode();
                return BTStatus.Failure; 
            }
            
            if (goblin.currentTarget == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            if (goblin.CheckNPCInAttackRange())
            {
                goblin.characterMovement.RequestStopMoving();
                goblin.currentState = UnitState.Idle;
                goblin.animState = AnimState.Idle;
                ResetNode();
                return BTStatus.Failure; 
            }

            if (!hasStartedMove)
            {
                var path = goblin.FindBestPathToAnyAdjacentWithoutDiagonal(
                    goblin.currentTarget.gameObject, 
                    goblin.currentTargetLayerIndex);
                
                if (path == null)
                {
                    ResetNode();
                    return BTStatus.Failure;
                }

                goblin.characterMovement.RequestStopMoving();
                goblin.MoveToTargetPosition(path);

                goblin.currentState = UnitState.Move;
                goblin.animState = AnimState.Moving;

                hasStartedMove = true;
                isAligningTarget = false;
                return BTStatus.Running;
            }

            if (!isAligningTarget && goblin.characterMovement.moving)
                return BTStatus.Running;

            if (!isAligningTarget && !goblin.characterMovement.moving)
            {
                if (!goblin.IsInAttackPosition()) 
                {
                    if (goblin.CheckTargetBuildingInAttackRange())
                    {
                        isAligningTarget = true;
                        return BTStatus.Running;
                    }
                    else
                    {
                        ResetNode();
                        return BTStatus.Running;
                    }
                }
                else
                {
                    FinishMove();
                    return BTStatus.Success;
                }
            }

            if (isAligningTarget)
            {
                if (!goblin.IsInAttackPosition())
                {
                    if (goblin.CheckTargetBuildingInAttackRange())
                    {
                        goblin.MoveDirectlyToTarget(goblin.currentTarget.gameObject);
                        return BTStatus.Running;
                    }
                    else
                    {
                        ResetNode();
                        return BTStatus.Running;
                    }
                }

                FinishMove();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            isAligningTarget = false;
        }

        private void FinishMove()
        {
            ResetNode();
        }
    }
}