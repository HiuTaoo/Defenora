using _Script.BT.Node;
using UnityEngine;

public class ReachedLastSeenPositionNode : BTActionNode
{
    private Warrior warrior;

    private float waitTime = 3f;
    private float timer = 0f;
    private bool hasArrived = false;

    public ReachedLastSeenPositionNode(Unit unit) : base(unit)
    {
        warrior = unit as Warrior;
    }

    public override BTStatus Tick()
    {
        Vector3Int targetCell = Vector3Int.FloorToInt(warrior.lastSeenPosition);
        targetCell.z = 0;
        var targetWorldPos = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);
        
        float dist = Vector2.Distance(
            warrior.transform.position,
            targetWorldPos
        );
        
        if (!hasArrived)
        {
            if (dist > 0.1f)
                return BTStatus.Running;

            hasArrived = true;
            timer = 0f;
            
            warrior.currentState = UnitState.Idle;
            warrior.animState = AnimState.Idle;
        }

        timer += Time.deltaTime;

        if (timer >= waitTime)
        {
            ResetNode();
            return BTStatus.Success;
        }

        return BTStatus.Running;
    }

    private void ResetNode()
    {
        hasArrived = false;
        timer = 0f;
    }
}