namespace _Script.BT.Node.WarriorNode.WarriorCombat.WarriorChaseEnemy
{
    public class IsEnemyOutOfWarriorAttackRangeNode: BTActionNode
    {
        private Warrior warrior;

        public IsEnemyOutOfWarriorAttackRangeNode(Unit unit) : base(unit)
        {
            this.warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.warriorBlackBoard.detectedEnemy == null)
                return BTStatus.Failure;
            
            return warrior.IsEnemyInAttackRange() ?  BTStatus.Failure : BTStatus.Success;
        }
    }
}