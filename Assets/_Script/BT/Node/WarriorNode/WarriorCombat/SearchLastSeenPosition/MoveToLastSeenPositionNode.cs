using UnityEngine;

namespace _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition
{
    public class MoveToLastSeenPositionNode : BTActionNode
    {
        private Warrior warrior;

        private bool hasStartedMove = false;
        private Vector3 targetWorldPos;

        public MoveToLastSeenPositionNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.currentTarget == null)
            {
                FinishMove();
                return BTStatus.Failure;
            }

            Vector3Int targetCell = Vector3Int.FloorToInt(warrior.lastSeenPosition);
            targetCell.z = 0;

            targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

            if (!hasStartedMove)
            {
                var floorAgent = warrior.GetComponentInChildren<FloorAgent>();
                var path = PathfindingAlgorithm.Instance.FindMultiLayerPath(
                    Vector3Int.FloorToInt(warrior.transform.position), 
                    floorAgent._currentFloorIndex
                    ,targetCell, 
                    warrior.lastSeenLayerIndex);
                if (path == null)
                {
                    FinishMove();
                    return BTStatus.Failure;
                }

                warrior.MoveToTargetPosition(path);
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

        private void FinishMove()
        {
            hasStartedMove = false;
            warrior.animState = AnimState.Idle;
        }
    }
}