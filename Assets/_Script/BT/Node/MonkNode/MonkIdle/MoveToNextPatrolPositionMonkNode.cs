using UnityEngine;

namespace _Script.BT.Node.MonkNode.MonkIdle
{
    public class MoveToNextPatrolPositionMonkNode: BTActionNode
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
            if (monk.monkBlackBoard.pathFinding == null)
                return BTStatus.Failure;
            var dir = monk.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (monk.DetectEnemies(monk.viewDistance, dir).Count != 0) 
            {
                FinishMove();
                return BTStatus.Failure;
            }
            
            Vector3Int targetCell = monk.monkBlackBoard.patrolTarget;

            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                monk.MoveToTargetPosition(monk.monkBlackBoard.pathFinding);
                hasStartedMove = true;

                monk.currentState = UnitState.Moving;
            }

            float dist = Vector2.Distance(monk.transform.position, targetWorldPos);

            bool isCloseEnough = dist < 0.2f; 
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
            monk.currentState = UnitState.Idle;
        }
    }
}