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

    private float flipTimer;
    private float nextFlipTime;

    public override BTStatus Tick()
    {
        if (!initialized)
        {
            waitTime = Random.Range(minTime, maxTime);
            timer = 0f;

            flipTimer = 0f;
            SetNextFlipTime();
            
            initialized = true;

            if (unit.characterMovement != null)
            {
                unit.characterMovement.StopMoving();
            }
        }

        unit.currentState = UnitState.Idle;
        unit.animState = AnimState.Idle;
        
        timer += Time.deltaTime;
        flipTimer += Time.deltaTime;

        if (flipTimer >= nextFlipTime)
        {
            if (unit != null)
            {
                var currentXScaleDirection = unit.transform.localScale.x;

                var reverseDirection = currentXScaleDirection > 0 ? Vector3.left : Vector3.right;

                unit.UpdateFacing(reverseDirection);
            }

            flipTimer = 0f;
            SetNextFlipTime();
        }

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
        flipTimer = 0f;
    }

    private void SetNextFlipTime()
    {
        nextFlipTime = Random.Range(1f, 3f);
    }
}