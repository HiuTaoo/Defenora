namespace _Script.BT.Node.WarriorNode.WarriorCombat
{
    public class IsEnemyInAttackRangeWarriorNode: BTActionNode
    {
        private Warrior warrior;

        public IsEnemyInAttackRangeWarriorNode(Unit unit) : base(unit)
        {
            this.warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            return warrior.IsEnemyInAttackRange() ?  BTStatus.Success : BTStatus.Failure;
        }
    }
}