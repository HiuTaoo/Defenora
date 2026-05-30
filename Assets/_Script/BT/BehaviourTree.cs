namespace _Script.BT
{
    public class BehaviourTree
    {
        private readonly BTNode root;

        public BehaviourTree(BTNode rootNode)
        {
            root = rootNode;
        }

        public void Tick()
        {
            root?.Tick();
        }

        public void ClearState()
        {
            root?.ClearState();
        }
    }
}