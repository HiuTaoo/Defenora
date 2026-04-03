using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.WarriorChaseEnemy
{
    public class WarriorChaseEnemy : BTActionNode
    {
        private Warrior warrior;
        private Vector2 lastTargetPosition;
        private float repathTimer = 0f;

        private bool hasStartedMove = false;
        private Transform currentTarget;

        public WarriorChaseEnemy(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior == null || warrior.warriorBlackBoard.detectedEnemy == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            currentTarget = warrior.warriorBlackBoard.detectedEnemy.transform;

            if (warrior.IsEnemyInAttackRange())
            {
                ResetNode();
                return BTStatus.Success;
            }

            var enemyFloorAgent = currentTarget.GetComponentInChildren<FloorAgent>();
            if (enemyFloorAgent == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            if (!hasStartedMove)
            {
                var path = warrior.FindBestPathToTarget(currentTarget.gameObject, enemyFloorAgent._currentFloorIndex);
                if (path == null)
                {
                    ResetNode();
                    Debug.Log("Cant find path");
                    return BTStatus.Failure;
                }
               
                warrior.currentState = UnitState.Move;
                warrior.MoveToTargetPosition(path);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (warrior.characterMovement.moving)
            {
                if (warrior.IsEnemyInAttackRange())
                {
                    ResetNode();
                    return BTStatus.Success;
                }

                return BTStatus.Running;
            }

            ResetNode();
            return BTStatus.Running;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            currentTarget = null;
            
            warrior.characterMovement.RequestStopMoving();
            warrior.animState = AnimState.Idle;
            warrior.currentState = UnitState.Idle;
        }
    }
}