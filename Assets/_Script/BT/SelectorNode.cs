using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorNode : BTComposite
{
    public SelectorNode(params BTNode[] nodes) : base(nodes) {}

    public override BTStatus Tick()
    {
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Tick();

            if (status != BTStatus.Failure)
                return status;
        }

        return BTStatus.Failure;
    }
}
