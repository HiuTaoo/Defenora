using _Script.Unit_Management_System.Enemy;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class HasTargetBuildingNode: BTActionNode
    {
        public HasTargetBuildingNode(Unit unit) : base(unit) { }
        
        public override BTStatus Tick()
        {
            if (unit.currentTarget == null)
                return BTStatus.Failure;
                
            var building = unit.currentTarget.GetComponent<Building>();
            
            if (building == null || building.buildingState == BuildingState.Destroyed)
            {
                unit.currentTarget = null;
                unit.ResetAnim(); 
                return BTStatus.Failure;
            }
            
            unit.currentTargetLayerIndex = building.layerIndex;
            return BTStatus.Success;
        }
    }
}