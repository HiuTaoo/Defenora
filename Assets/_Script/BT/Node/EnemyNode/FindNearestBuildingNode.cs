using _Script.Unit_Management_System.Enemy;
using UnityEngine;

namespace _Script.BT.Node.EnemyNode.unitNode
{
    public class FindNearestBuildingNode : BTActionNode
    {
        
        public FindNearestBuildingNode(Unit unit) : base(unit) { }

        public override BTStatus Tick()
        {
            if (unit.currentTarget != null && unit.currentTarget.CompareTag("Building"))
            {
                var building = unit.currentTarget.GetComponent<Building>();
                if (building != null && building.buildingState != BuildingState.Destroyed)
                {
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

            unit.currentTarget = null;
            return BTStatus.Failure;
        }
    }
}