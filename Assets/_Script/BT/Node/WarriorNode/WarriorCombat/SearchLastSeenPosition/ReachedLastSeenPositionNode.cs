using _Script.BT.Node;
using UnityEngine;

public class ReachedLastSeenPositionNode : BTActionNode
{
    private Warrior warrior;

    private float waitTime = 1.5f;
    private float timer = 0f;
    private bool hasArrived = false;

    public ReachedLastSeenPositionNode(Unit unit) : base(unit)
    {
        warrior = unit as Warrior;
    }

    public override BTStatus Tick()
    {
        float dist = Vector2.Distance(
            warrior.transform.position,
            warrior.lastSeenPosition
        );
        
        if (!hasArrived)
        {
            if (dist > 0.2f)
                return BTStatus.Running;

            hasArrived = true;
            timer = 0f;
            Debug.Log("Reach Last Seen Position");
            
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