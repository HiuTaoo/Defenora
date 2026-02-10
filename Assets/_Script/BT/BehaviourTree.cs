using UnityEngine;

namespace _Script.BT
{
    public class BehaviourTree
    {
        private BTNode root;

        public BehaviourTree(BTNode rootNode)
        {
            root = rootNode;
        }

        public void Tick()
        {
            root?.Tick();
        }

    }

}