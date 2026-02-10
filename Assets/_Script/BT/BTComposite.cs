using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BTComposite : BTNode
{
    protected List<BTNode> children = new List<BTNode>();

    public BTComposite(params BTNode[] nodes)
    {
        children.AddRange(nodes);
    }
}
