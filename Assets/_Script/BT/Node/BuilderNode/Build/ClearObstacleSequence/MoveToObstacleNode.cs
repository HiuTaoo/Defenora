using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Build.ClearObstacleSequence
{
    public class MoveToObstacleNode: BTActionNode
    {
        public MoveToObstacleNode(Builder builder): base(builder){}
        
        private bool hasStartedMove = false;
        private bool isAligningTarget = false;
        private bool hasMovedAtLeastOnce = false;

        
        public override BTStatus Tick()
        {
            if (builder.currentTask == null || builder.builderBlackBoard.pathFinding == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }
            
            var obstacleGO = (builder.builderBlackBoard.currentObstacle as Component)?.gameObject;

            // ===== START =====
            if (!hasStartedMove)
            {
                builder.animFSM.SetTool(builder.currentTool);
                builder.animFSM.SetResource(builder.currentResource);
                builder.animFSM.ChangeState(UnitState.Moving);

                builder.MoveToTargetPosition(builder.builderBlackBoard.pathFinding);

                hasStartedMove = true;
                isAligningTarget = false;
                return BTStatus.Running;
            }
            
            // ===== PHASE 1: FOLLOW PATH =====
            if (!isAligningTarget)
            {
                if (builder.characterMovement.moving)
                {
                    hasMovedAtLeastOnce = true;
                    return BTStatus.Running;
                }

                // CHỈ coi là hết path nếu đã từng di chuyển
                if (hasMovedAtLeastOnce)
                {
                    if (!builder.IsCollidingWithTarget(obstacleGO))
                    {
                        isAligningTarget = true;
                        return BTStatus.Running;
                    }
                }
            }

            // ===== END PATH → CHECK COLLISION =====
            if (!isAligningTarget && !builder.characterMovement.moving)
            {
                if (!builder.IsCollidingWithTarget(obstacleGO))
                {
                    isAligningTarget = true;
                    return BTStatus.Running;
                }
            }

            // ===== PHASE 2: ALIGN X =====
            if (isAligningTarget)
            {
                if (!builder.IsCollidingWithTarget(obstacleGO))
                {
                    
                    builder.MoveHorizontallyToTarget(obstacleGO);
                    return BTStatus.Running;
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
            hasMovedAtLeastOnce = false;
        }


        private void FinishMove()
        {
            ResetNode();
            builder.currentState = UnitState.Idle;
            builder.animFSM.ChangeState(UnitState.Idle);
        }
    }
}