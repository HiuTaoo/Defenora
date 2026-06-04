using System.Collections.Generic;

namespace _Script.BT
{
    public class SelectorNode : BTComposite
    {
        public SelectorNode(params BTNode[] nodes) : base(nodes) {}

        public override BTStatus Tick()
        {
            for (var i = 0; i < children.Count; i++)
            {
                var status = children[i].Tick();

                if (status != BTStatus.Failure)
                {
                    for (int j = i + 1; j < children.Count; j++)
                    {
                        children[j].ClearState(); 
                    }
                    
                    return status;
                }
            }

            return BTStatus.Failure;
        }
    }
}