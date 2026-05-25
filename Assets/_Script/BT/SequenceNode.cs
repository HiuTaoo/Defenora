using System.Collections.Generic;
using UnityEngine;

public class SequenceNode : BTComposite
{
    private int currentIndex = 0;
    public SequenceNode(params BTNode[] nodes) : base(nodes) {}
    public override BTStatus Tick()
    {
        while (currentIndex < children.Count)
        {
            var status = children[currentIndex].Tick();
            if (status == BTStatus.Running)
                return BTStatus.Running;

            if (status == BTStatus.Failure)
            {
                currentIndex = 0;
                return BTStatus.Failure;
            }
            currentIndex++;
        }
        currentIndex = 0;
        return BTStatus.Success;
    }
    
    public override void ClearState()
    {
        currentIndex = 0;
        foreach (var child in children)
        {
            child.ClearState();
        }
    }
}