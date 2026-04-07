namespace _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding
{
    public class IsWarriorInDefendState: BTActionNode
    {
        private Warrior warrior;

        public IsWarriorInDefendState(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            if(warrior.currentState == UnitState.Defend || warrior.currentState == UnitState.Move)
                return BTStatus.Success;
            return BTStatus.Failure;
        }
    }
}