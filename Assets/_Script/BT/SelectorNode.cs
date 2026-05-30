public class SelectorNode : BTComposite
{
    public SelectorNode(params BTNode[] nodes) : base(nodes)
    {
    }

    public override BTStatus Tick()
    {
        var intermediateNodeFound = false;

        for (var i = 0; i < children.Count; i++)
        {
            if (intermediateNodeFound)
            {
                children[i].ClearState();
                continue;
            }

            var status = children[i].Tick();

            if (status != BTStatus.Failure) intermediateNodeFound = true;
        }

        for (var i = 0; i < children.Count; i++)
        {
            var status = children[i].Tick();
            if (status != BTStatus.Failure) return status;
        }

        return BTStatus.Failure;
    }
}