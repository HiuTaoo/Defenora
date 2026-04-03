using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorIdle
{
    public class WarriorMoveToNextPatrolPositionNode:BTActionNode
    {
        private Warrior warrior;
        
        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public WarriorMoveToNextPatrolPositionNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }
        public override BTStatus Tick()
        {
            if (warrior.warriorBlackBoard.pathFinding == null)
                return BTStatus.Failure;
            var dir = warrior.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            if (warrior.DetectEnemies(warrior.viewDistance, dir).Count != 0) 
            {
                FinishMove();
                return BTStatus.Failure;
            }
            
            Vector3Int targetCell = warrior.warriorBlackBoard.patrolTarget;

            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                warrior.MoveToTargetPosition(warrior.warriorBlackBoard.pathFinding);
                hasStartedMove = true;

                warrior.currentState = UnitState.Move;
                warrior.animState = AnimState.Moving;
            }

            float dist = Vector2.Distance(warrior.transform.position, targetWorldPos);

            bool isCloseEnough = dist < 0.2f; 
            bool isStopped = warrior.IsStopped();

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
            warrior.currentState = UnitState.Idle;
        }
    }
}