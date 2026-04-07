namespace _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding
{
    public class WarriorStopMovingNode: BTActionNode
    {
        private Warrior warrior;

        public WarriorStopMovingNode(Unit unit) : base(unit)
        {
            warrior = unit as Warrior;
        }

        public override BTStatus Tick()
        {
            warrior.characterMovement.RequestStopMoving();
            warrior.animState = AnimState.Idle;
            return BTStatus.Success;
        }
    }
}