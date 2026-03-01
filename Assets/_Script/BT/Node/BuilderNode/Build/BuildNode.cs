using _Script.Unit_Management_System.Animation;

namespace _Script.BT.Node.BuilderNode.Build
{
    public class BuildNode : BTActionNode
    {
        public BuildNode(Builder builder) :  base(builder) { }

        public override BTStatus Tick()
        {
            if (builder.currentState != UnitState.Working)
            {
                builder.currentState = UnitState.Working;
                builder.currentTool = ToolType.Hammer;
                builder.UpdateAnim();
                builder.animFSM.ChangeState(UnitState.Working);
            }
            
            var building = builder.currentTask?.targetGameObject?.GetComponent<Building>();
            
            if (builder.IsCompletedBuild())
            {
                builder.currentState = UnitState.Idle;
                builder.animFSM.ChangeState(UnitState.Idle);
                builder.currentTask = null;
                return BTStatus.Success;
            }			
			
            return BTStatus.Running;
        }
    }
}