namespace _Script.BT.Node.WarriorNode.WarriorCombat.WarriorChaseEnemy
{
    public class HasEnemyInWarriorSight: BTActionNode
    {
        private Warrior warrior;

        public HasEnemyInWarriorSight(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (warrior.warriorBlackBoard.detectedEnemy != null &&
                warrior.CheckEnemyStillInRange(warrior.warriorBlackBoard.detectedEnemy,
                    warrior.viewDistance))
            {
                return BTStatus.Success;
            }
            return BTStatus.Failure;
        }
    }
}