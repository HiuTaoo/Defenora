namespace _Script.BT.Node.EnemyNode
{
    public class IsNightStartNode: BTActionNode
    {
        public IsNightStartNode(Unit unit):base(unit){}

        public override BTStatus Tick()
        {
            return TimeOfDaySystem.Instance.GetCurrentTime() is >= 0 and < 6 ? BTStatus.Success : BTStatus.Failure;
        }
    }
}