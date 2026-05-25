using _Script.BT;
using _Script.Task;

public class HasCurrentTaskOfTypeNode : BTConditionNode
{
    private TaskType targetType;

    public HasCurrentTaskOfTypeNode(Builder builder, TaskType type) : base(builder)
    {
        this.targetType = type;
    }

    public override BTStatus Tick()
    {
        if (builder != null && builder.currentTask != null)
        {
            if (!builder.currentTask.IsCompleted && builder.currentTask.taskType == targetType)
            {
                return BTStatus.Success;
            }
        }

        return BTStatus.Failure;
    }
}