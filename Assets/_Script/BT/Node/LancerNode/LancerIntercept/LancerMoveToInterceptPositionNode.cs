using UnityEngine;
using _Script.BT.BlackBoard;

namespace _Script.BT.Node.LancerNode.LancerIntercept
{
    public class LancerMoveToInterceptPositionNode : BTActionNode
    {
        private Lancer lancer;

        private bool hasStartedMove = false;

        public LancerMoveToInterceptPositionNode(Unit unit) : base(unit)
        {
            this.lancer = unit as Lancer;
        }

        public override BTStatus Tick()
        {
            //var enemy = lancer.lancerBlackBoard.detectedEnemy;
            var building = lancer.assignedBuilding;

            if (lancer.lastSeenPosition == Vector2.zero || building == null)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            if (lancer.IsEnemyInAttackRange())
            {
                ResetNode();
                return BTStatus.Success;
            }

            if (!hasStartedMove)
            {
                Vector2 buildingPos = building.transform.position;
                Vector2 enemyPos = lancer.lastSeenPosition ;
                float guardRadius = lancer.maxRadius; 

                Vector2 vectorToEnemy = enemyPos - buildingPos;
                Vector2 clampedVector = Vector2.ClampMagnitude(vectorToEnemy, guardRadius);
                Vector2 targetPos = buildingPos + clampedVector;

                Vector3Int targetCell = Vector3Int.FloorToInt(targetPos);
                targetCell.z = 0;
                
                Vector3Int startCell = Vector3Int.FloorToInt(lancer.transform.position);
                startCell.z = 0;

                if (startCell == targetCell)
                {
                    ResetNode();
                    return BTStatus.Success;
                }

                var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                    startCell, 
                    lancer.floorAgent._currentFloorIndex,
                    targetCell,
                    building.layerIndex);

                if (path == null || !path.isValid)
                {
                    ResetNode();
                    return BTStatus.Success; 
                }

                lancer.lancerBlackBoard.pathFinding = path;
                lancer.MoveToTargetPosition(path);
                
                hasStartedMove = true;
                lancer.currentState = UnitState.Move;
                lancer.animState = AnimState.Moving;

                return BTStatus.Running;
            }

            if (lancer.characterMovement != null && lancer.characterMovement.moving)
            {
                return BTStatus.Running;
            }

            ResetNode();
            return BTStatus.Running;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            
            if (lancer.characterMovement != null)
            {
                lancer.characterMovement.RequestStopMoving();
            }

            lancer.currentState = UnitState.Idle;
            lancer.animState = AnimState.Idle;
        }
    }
}