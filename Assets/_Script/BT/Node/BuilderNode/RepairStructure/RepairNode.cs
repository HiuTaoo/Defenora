using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode.RepairStructure
{
    public class RepairNode: BTActionNode
    {
        private Builder builder;
        public RepairNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentState != UnitState.Working)
            {
                builder.currentState = UnitState.Working;
                builder.animState = AnimState.Working;
                builder.currentTool = ToolType.Hammer;
                builder.currentResource = ResourceType.None;
                builder.UpdateAnim();
            }
            
            if (builder.IsCompletedRepair())
            {
                builder.ResetState();
                return BTStatus.Success;
            }			
			
            return BTStatus.Running;
        }
    }
    
}