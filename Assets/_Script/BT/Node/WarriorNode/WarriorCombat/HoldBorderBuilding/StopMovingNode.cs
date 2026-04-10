namespace _Script.BT.Node.WarriorNode.WarriorCombat.ReturnToBuilding
{
    public class StopMovingNode: BTActionNode
    {
        public StopMovingNode(Unit unit) : base(unit) { }
        
        public override BTStatus Tick()
        {
            unit.characterMovement.RequestStopMoving();
            unit.animState = AnimState.Idle;
            return BTStatus.Success;
        }
    }
}