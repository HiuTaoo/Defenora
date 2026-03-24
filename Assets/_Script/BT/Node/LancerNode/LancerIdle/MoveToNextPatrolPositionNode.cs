using UnityEngine;

namespace _Script.BT.Node.LancerNode.LancerIdle
{
    public class MoveToNextPatrolPositionNode : BTActionNode
    {
        private Lancer lancer;

        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public MoveToNextPatrolPositionNode(Unit unit) : base(unit)
        {
            lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            if (lancer.lancerBlackBoard.pathFinding == null)
                return BTStatus.Failure;
            var dir = lancer.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (lancer.DetectEnemies(lancer.viewDistance, dir).Count != 0) 
            {
                FinishMove();
                return BTStatus.Failure;
            }
            
            Vector3Int targetCell = lancer.lancerBlackBoard.patrolTarget;

            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                lancer.MoveToTargetPosition(lancer.lancerBlackBoard.pathFinding);
                hasStartedMove = true;

                lancer.currentState = UnitState.Moving;
            }

            float dist = Vector2.Distance(lancer.transform.position, targetWorldPos);

            bool isCloseEnough = dist < 0.2f; 
            bool isStopped = lancer.IsStopped();

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
            lancer.currentState = UnitState.Idle;
        }
    }
}