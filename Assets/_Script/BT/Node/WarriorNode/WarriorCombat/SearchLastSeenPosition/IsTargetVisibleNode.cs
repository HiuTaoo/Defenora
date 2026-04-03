namespace _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition
{
    public class IsTargetVisibleNode: BTActionNode
    {
        private Warrior warrior;

        public IsTargetVisibleNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (!warrior.CheckEnemyStillInRange(warrior.currentTarget.gameObject, warrior.viewDistance))
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}