namespace _Script.BT.Node.EnemyNode
{
    public class DespawnNode: BTActionNode
    {
        public DespawnNode(Unit unit): base(unit){}

        public override BTStatus Tick()
        {
            PoolManager.Instance.Despawn(unit.gameObject);
            if (UnitManager.Instance.enemies.Contains(unit))
                UnitManager.Instance.enemies.Remove(unit);
            return BTStatus.Success;
        }
    }
}