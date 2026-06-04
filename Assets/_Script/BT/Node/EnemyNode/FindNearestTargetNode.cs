using _Script.Enum;

namespace _Script.BT.Node.EnemyNode.unitNode
{
    public class FindNearestTargetNode : BTActionNode
    {
        public FindNearestTargetNode(Unit unit) : base(unit)
        {
        }

        public override BTStatus Tick()
        {
            if (unit.currentTarget != null && unit.currentTarget.gameObject.activeInHierarchy)
            {
                if (unit.currentTarget.CompareTag("Building"))
                {
                    var building = unit.currentTarget.GetComponent<Building>();
                    if (building != null && building.buildingState != BuildingState.Destroyed) return BTStatus.Success;
                }
                else 
                {
                    var targetUnit = unit.currentTarget.GetComponent<Unit>();
                    
                    if (targetUnit != null) 
                    {
                        unit.currentTargetLayerIndex = targetUnit.characterMovement.CurrentLayer;
                    }
                    else if (PlayerController.Instance != null)
                    {
                        unit.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;
                    }

                    return BTStatus.Success;
                }
            }

            Building nearestBuilding = unit.FindNearestBuilding(unit.transform.position);
            if (nearestBuilding != null)
            {
                unit.currentTarget = nearestBuilding.transform;
                unit.currentTargetLayerIndex = nearestBuilding.layerIndex;
                return BTStatus.Success;
            }

            var allNPCs = unit.DetectAllNPCsInRange(unit.viewDistance);
            if (allNPCs != null && allNPCs.Count > 0)
            {
                var nearestNPC = unit.SelectClosestTarget(allNPCs);
                if (nearestNPC != null)
                {
                    unit.currentTarget = nearestNPC.transform;
                    var npcUnit = nearestNPC.GetComponent<Unit>();
                    unit.currentTargetLayerIndex = npcUnit != null ? npcUnit.characterMovement.CurrentLayer : unit.characterMovement.CurrentLayer;
                    return BTStatus.Success;
                }
            }

            if (PlayerController.Instance != null && PlayerController.Instance.gameObject.activeInHierarchy)
            {
                unit.currentTarget = PlayerController.Instance.transform;
                if (PlayerController.Instance.floorAgent != null)
                    unit.currentTargetLayerIndex = PlayerController.Instance.floorAgent.currentFloorIndex;
                return BTStatus.Success;
            }

            unit.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}