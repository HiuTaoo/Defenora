using _Script.BT.Node;
using UnityEngine;

public class StationedLookAtAlarmNode : BTActionNode
{
    private readonly Archer archer;

    public StationedLookAtAlarmNode(Unit unit) : base(unit)
    {
        archer = unit as Archer;
    }

    public override BTStatus Tick()
    {
        if (archer == null || !archer.isAlerted || archer.lastSeenPosition == Vector2.zero) return BTStatus.Failure;

        if (archer.archerBlackBoard.detectedEnemy != null) return BTStatus.Failure;

        var directionToAlarm = (Vector3)archer.lastSeenPosition - archer.transform.position;

        var facingDir = directionToAlarm.x > 0 ? Vector2.right : Vector2.left;
        archer.UpdateFacing(facingDir);

        archer.currentState = UnitState.Idle;
        archer.animState = AnimState.Idle;

        return BTStatus.Success;
    }
}