using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_ChopState : IUnitState
{
    private BuilderController pawn;
    private bool isCompleted = false;

    public Builder_ChopState(BuilderController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Chop");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() { }

    public void Update()
    {
        if (isCompleted)
        {
            pawn.StateMachine.ChangeState(new Builder_IdleState(pawn));
            isCompleted = false;
        }
    }

    public void FixedUpdate() { }

    public void SetCompleted()
    {
        isCompleted = true;
    }
}
