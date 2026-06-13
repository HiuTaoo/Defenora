using UnityEngine;

namespace _Script.BT.Node.unitNode.unitCombat.SearchLastSeenPosition
{
    public class MoveToLastSeenPositionNode : BTActionNode
    {
        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public MoveToLastSeenPositionNode(Unit unit) : base(unit) { }

        public override BTStatus Tick()
        {
            if (unit.currentTarget == null && !unit.isAlerted)
            {
                FinishMove();
                return BTStatus.Failure;
            }

            var targetCell = GraphNode.Instance.WorldToGridPos(unit.lastSeenPosition, unit.lastSeenLayerIndex);
            targetCell.z = 0;

            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                var pos = GraphNode.Instance.WorldToGridPos(unit.transform.position, unit.layerIndex);
                var floorAgent = unit.GetComponentInChildren<FloorAgent>();
                var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                    pos, 
                    floorAgent._currentFloorIndex
                    ,targetCell, 
                    unit.lastSeenLayerIndex);
                if (path == null)
                {
                    FinishMove();
                    return BTStatus.Failure;
                }
                
                unit.characterMovement.RequestStopMoving();
                unit.MoveToTargetPosition(path);
                hasStartedMove = true;

                unit.currentState = UnitState.Move;
                unit.animState = AnimState.Moving;
            }

            float dist = Vector2.Distance(unit.transform.position, targetWorldPos);
            bool isCloseEnough = dist < 0.1f;
            bool isStopped = unit.IsStopped();

            if (isCloseEnough || isStopped)
            {
                FinishMove();
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private void FinishMove()
        {
            hasStartedMove = false;
            unit.animState = AnimState.Idle;
        }
    }
}