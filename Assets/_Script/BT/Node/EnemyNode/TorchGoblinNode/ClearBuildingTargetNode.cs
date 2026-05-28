using _Script.Enum;

namespace _Script.BT.Node.EnemyNode.TorchGoblinNode
{
    public class ClearBuildingTargetNode: BTActionNode
    {
        public ClearBuildingTargetNode(Unit unit) : base(unit) { }

        public override BTStatus Tick()
        {
            if(unit.currentTarget == null)
                return BTStatus.Failure;
            var buidling = unit.currentTarget.GetComponent<Building>();
            if(buidling == null || buidling.buildingState != BuildingState.Destroyed)
                return BTStatus.Failure;
            
            unit.currentTarget = null;
            unit.ResetAnim();
            return BTStatus.Success;
        }
    }
}