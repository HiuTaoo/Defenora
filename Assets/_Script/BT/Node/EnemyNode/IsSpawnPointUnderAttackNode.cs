using _Script.BT.Node;

public class IsSpawnPointUnderAttackNode : BTActionNode
{
    public IsSpawnPointUnderAttackNode(Unit unit) : base(unit)
    {
    }

    public override BTStatus Tick()
    {
        if (unit.enemySpawnPoint != null)
        {
            var spawnPoint = unit.enemySpawnPoint.GetComponent<SpawnPoint>();
            if (spawnPoint != null && spawnPoint.isAttacked) return BTStatus.Success;
        }

        return BTStatus.Failure;
    }
}