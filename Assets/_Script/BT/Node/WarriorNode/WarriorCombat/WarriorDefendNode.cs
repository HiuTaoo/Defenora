namespace _Script.BT.Node.WarriorNode.WarriorCombat
{
    public class WarriorDefendNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorDefendNode(Unit unit) : base(unit)
        {
            this.warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if (!warrior.CheckEnemyStillInRange(warrior.viewDistance))
                return BTStatus.Failure;
            
            warrior.animState = AnimState.Defending;
            return BTStatus.Running;
        }
    }
}