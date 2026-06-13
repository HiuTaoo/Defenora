using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.BarrelNode
{
    public class BarrelMoveToTargetNode : BTActionNode
    {
        private Barrel barrel;
        private bool hasStartedMove = false;
        private int currentPathLayerIndex = -1;

        public BarrelMoveToTargetNode(Unit unit) : base(unit)
        {
            barrel = unit as Barrel;
        }

        public override BTStatus Tick()
        {
            if (barrel == null || barrel.isKnockedBack || barrel.currentState == UnitState.Dead || 
                barrel.currentTarget == null || barrel.currentTarget.gameObject == null || barrel.currentState == UnitState.Attack)
            {
                ResetNode();
                return BTStatus.Failure;
            }

            var targetUnit = barrel.currentTarget.GetComponent<Unit>();
            if (targetUnit != null)
            {
                barrel.currentTargetLayerIndex = targetUnit.characterMovement.CurrentLayer;
            }
            else if (PlayerController.Instance != null)
            {
                barrel.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;
            }

            if (barrel.IsCollidingWithTarget(barrel.currentTarget.gameObject))
            {
                StopBarrel();
                ResetNode();
                return BTStatus.Success;
            }

            if (hasStartedMove && barrel.currentTarget.CompareTag("Building") && 
                barrel.characterMovement != null && !barrel.characterMovement.moving)
            {
                var buildingCol = barrel.currentTarget.GetComponent<Collider2D>();
                float distToEdge = float.MaxValue;

                if (buildingCol != null)
                    distToEdge = Vector2.Distance(barrel.transform.position, buildingCol.ClosestPoint(barrel.transform.position));

                if (distToEdge <= 1.05f) 
                {
                    StopBarrel();
                    ResetNode();
                    return BTStatus.Success;
                }
            }

            barrel.currentState = UnitState.Move;
            barrel.animState = AnimState.Moving;

            if (currentPathLayerIndex != -1 && currentPathLayerIndex != barrel.currentTargetLayerIndex)
            {
                hasStartedMove = false;
            }

            if (!hasStartedMove)
            {
                var targetGridPos = GraphNode.Instance.WorldToGridPos(barrel.currentTarget.position, barrel.layerIndex);
                targetGridPos.z = 0;

                PathFinding path = null;
                var isTargetNodeWalkable = GraphNode.Instance.IsWalkableNode(targetGridPos, barrel.currentTargetLayerIndex);

                if (!isTargetNodeWalkable)
                {
                    path = barrel.FindBestPathToAnyAdjacentWithoutDiagonal(
                        barrel.currentTarget.gameObject,
                        barrel.currentTargetLayerIndex);
                }
                else
                {
                    path = barrel.FindBestPathToTarget(
                        barrel.currentTarget.gameObject,
                        barrel.currentTargetLayerIndex);
                }

                if (path != null && path.segments != null && path.segments.Count > 0)
                {
                    barrel.characterMovement.RequestStopMoving();
                    barrel.MoveToTargetPosition(path);
                    
                    hasStartedMove = true;
                    currentPathLayerIndex = barrel.currentTargetLayerIndex;
                }
                else
                {
                    if (barrel.currentTarget != null)
                    {
                        barrel.MoveDirectlyToTarget(barrel.currentTarget.gameObject);
                    }
                }

                return BTStatus.Running;
            }

            if (barrel.characterMovement != null && barrel.characterMovement.moving)
            {
                return BTStatus.Running;
            }

            hasStartedMove = false;
            
            if (barrel.currentTarget != null)
            {
                barrel.MoveDirectlyToTarget(barrel.currentTarget.gameObject);
            }
            return BTStatus.Running;
        }

        private void StopBarrel()
        {
            barrel.characterMovement.RequestStopMoving();
            barrel.currentState = UnitState.Idle;
            barrel.animState = AnimState.Idle;
        }

        private void ResetNode()
        {
            hasStartedMove = false;
            currentPathLayerIndex = -1;
        }
    }
}