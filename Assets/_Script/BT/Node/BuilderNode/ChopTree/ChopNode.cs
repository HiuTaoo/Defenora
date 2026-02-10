using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode
{
    public class ChopNode : BTActionNode
    {
        public ChopNode(Builder builder) : base(builder) { }

        public override BTStatus Tick()
        {
            if (builder.currentState != UnitState.Working)
            {
				builder.currentState = UnitState.Working;
				builder.currentTool = ToolType.Axe;
				builder.animFSM.SetTool(builder.currentTool);
				builder.animFSM.SetResource(ResourceType.None);
				builder.animFSM.ChangeState(UnitState.Working);
            }

            if (builder.IsChopped())
			{
    			builder.currentState = UnitState.Idle;
			    builder.animFSM.ChangeState(UnitState.Idle);
			    builder.targetGO = null;
			    builder.builderBlackBoard.pathFinding = null;

    			return BTStatus.Success;
			}			
			
            return BTStatus.Running;
        }
    }
}