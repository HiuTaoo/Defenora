using _Script.Unit_Management_System.HealthComponent;
using UnityEngine;

namespace _Script.BT.Node.MonkNode.MonkIdle
{
    public class MoveToNextPatrolPositionMonkNode : BTActionNode
    {
        private Monk monk;
        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public MoveToNextPatrolPositionMonkNode(Unit unit) : base(unit)
        {
            monk = unit as Monk;
        }
        
        public override BTStatus Tick()
        {
            if (monk == null || monk.monkBlackBoard.pathFinding == null)
                return BTStatus.Failure;

            var allyLayerMask = LayerMask.GetMask("NPC");
            var allyCount = Physics2D.OverlapCircleNonAlloc(monk.transform.position, monk.viewDistance, monk.results,
                allyLayerMask);

            for (var i = 0; i < allyCount; i++)
            {
                var hit = monk.results[i];
                if (hit == null || hit.gameObject == monk.gameObject) continue;

                var allyHealth = hit.GetComponentInChildren<Health>();
                if (allyHealth != null && allyHealth.CurrentHealth < allyHealth.maxHealth &&
                    allyHealth.CurrentHealth > 0)
                {
                    FinishMove();
                    return
                        BTStatus.Failure;
                }
            }

            Vector3Int targetCell = monk.monkBlackBoard.patrolTarget;
            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                monk.MoveToTargetPosition(monk.monkBlackBoard.pathFinding);
                hasStartedMove = true;

                monk.currentState = UnitState.Move;
                monk.animState = AnimState.Moving;
            }

            float dist = Vector2.Distance(monk.transform.position, targetWorldPos);

            var isCloseEnough = dist < 0.2f;
            bool isStopped = monk.IsStopped();

            if (isCloseEnough || isStopped)
            {
                FinishMove();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        public void FinishMove()
        {
            hasStartedMove = false;
            if (monk.characterMovement != null) monk.characterMovement.RequestStopMoving();
            monk.currentState = UnitState.Idle;
            monk.animState = AnimState.Idle;
        }
    }
}