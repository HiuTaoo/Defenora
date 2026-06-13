using _Script.Enum;
using UnityEngine;

namespace _Script.BT.Node.ArcherNode.ArcherIdle
{
    public class MoveToNearestBuildingActionNode : BTActionNode
    {
        private const float FIND_BUILDING_RADIUS = 30f;
        private const string BUILDING_TAG = "Building";
        private readonly Archer archer;
        private bool hasStartedMove;
        private Vector3 targetGridPos;
        private int targetLayer; 

        public MoveToNearestBuildingActionNode(Unit unit) : base(unit)
        {
            archer = (Archer)unit;
        }

        public override BTStatus Tick()
        {
            if (hasStartedMove && archer.characterMovement.moving)
            {
                archer.currentState = UnitState.Move;
                archer.animState = AnimState.Moving;
                return BTStatus.Running;
            }

            if (!hasStartedMove)
            {
                if (archer.archerBlackBoard.nearestBuilding == null)
                    archer.archerBlackBoard.nearestBuilding = FindNearestBuilding();

                if (archer.archerBlackBoard.nearestBuilding == null)
                    return BTStatus.Failure;

                var building = archer.archerBlackBoard.nearestBuilding.GetComponent<Building>();
                if (building == null)
                    building = archer.archerBlackBoard.nearestBuilding.GetComponentInChildren<Building>();

                if (building == null) return BTStatus.Failure;

                var currentDistanceToBuilding = Vector2.Distance(archer.transform.position,
                    archer.archerBlackBoard.nearestBuilding.transform.position);
                var safeRadius = 6.0f;
                if (currentDistanceToBuilding <= safeRadius)
                {
                    hasStartedMove = false;
                    archer.currentState = UnitState.Idle;
                    archer.animState = AnimState.Idle;
                    return BTStatus.Success;
                }

                targetGridPos = building.GetRandomPositionAroundBuilding();
                targetLayer = building.layerIndex;

                archer.currentState = UnitState.Move;
                archer.animState = AnimState.Moving;
                var pos = GraphNode.Instance.WorldToGridPos(targetGridPos, targetLayer);
                archer.characterMovement.MoveToPosition(pos, targetLayer);

                hasStartedMove = true;
                return BTStatus.Running;
            }

            if (!archer.characterMovement.moving)
            {
                Debug.Log($"[🚨 ARCHER NIGHT] ✨ {archer.gameObject.name} đã đi bộ về nhà an toàn ẩn nấp!");
                hasStartedMove = false;

                archer.currentState = UnitState.Idle;
                archer.animState = AnimState.Idle;
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }

        private GameObject FindNearestBuilding()
        {
            var hits = Physics2D.OverlapCircleAll(archer.transform.position, FIND_BUILDING_RADIUS);
            GameObject nearest = null;
            var minDistance = Mathf.Infinity;

            foreach (var hit in hits)
                if (hit.CompareTag(BUILDING_TAG))
                {
                    var building = hit.GetComponent<Building>();
                    if (building != null && building.buildingState != BuildingState.Completed)
                        continue;
                    
                    var distance = Vector2.Distance(archer.transform.position, hit.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = hit.gameObject;
                    }
                }

            return nearest;
        }
    }
}