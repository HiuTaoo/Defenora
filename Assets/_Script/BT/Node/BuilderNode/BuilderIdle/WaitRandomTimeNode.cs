using _Script.BT.Node;
using UnityEngine;

public class WaitRandomTimeNode : BTActionNode
{
    public WaitRandomTimeNode(Unit unit) : base(unit)
    {
    }

    private float waitTime;
    private float timer;
    private bool initialized;

    private readonly float minTime = 2f;
    private readonly float maxTime = 5f;

    public override BTStatus Tick()
    {
        if (!initialized)
        {
            waitTime = Random.Range(minTime, maxTime);
            timer = 0f;
            initialized = true;

            if (unit.characterMovement != null)
            {
                unit.characterMovement.StopMoving();
            }
        }

        unit.currentState = UnitState.Idle;
        unit.animState = AnimState.Idle;
        timer += Time.deltaTime;

        if (timer >= waitTime)
        {
            initialized = false;
            return BTStatus.Success;
        }

        return BTStatus.Running;
    }

    public override void ClearState()
    {
        base.ClearState();
        initialized = false;
        timer = 0f;
    }
}