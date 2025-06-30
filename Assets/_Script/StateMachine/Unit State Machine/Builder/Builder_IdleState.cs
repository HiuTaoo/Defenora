using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_IdleState : IUnitState
{
    private BuilderController pawn;

    public Builder_IdleState(BuilderController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Idle");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() { }

    public void Update()
    {
        if (pawn.characterMovement.moving)
        {
            pawn.StateMachine.ChangeState(new Builder_WalkState(pawn));
        }

        if(pawn.MovementInput != Vector2.zero)
        {
            pawn.StateMachine.ChangeState(new Builder_WalkState(pawn));
        }


        /*if (Input.GetMouseButtonDown(0))
        {
            pawn.StateMachine.ChangeState(new ChopState(pawn));
        }*/
    }

    public void FixedUpdate() { }
}


