namespace _Script.BT.Node.EnemyNode
{
    public class DespawnNode: BTActionNode
    {
        public DespawnNode(Unit unit): base(unit){}

        public override BTStatus Tick()
        {
            PoolManager.Instance.Despawn(unit.gameObject);
            return BTStatus.Success;
        }
    }
}