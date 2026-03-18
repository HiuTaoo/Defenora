using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode.Build
{
    public class BuildNode : BTActionNode
    {
        private Builder builder;
        public BuildNode(Unit unit) : base(unit)
        {
            builder = (Builder)unit;
        }

        public override BTStatus Tick()
        {
            if (builder.currentState != UnitState.Working)
            {
                builder.currentState = UnitState.Working;
                builder.currentTool = ToolType.Hammer;
                builder.currentResource = ResourceType.None;
                builder.UpdateAnim();
                builder.animFSM.ChangeState(UnitState.Working);
            }
            
            var building = builder.currentTask?.targetGameObject?.GetComponent<Building>();
            
            if (builder.IsCompletedBuild())
            {
                builder.ResetState();
                return BTStatus.Success;
            }			
			
            return BTStatus.Running;
        }
    }
}