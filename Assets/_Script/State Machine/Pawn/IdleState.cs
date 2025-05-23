using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    private PawnController pawn;

    public IdleState(PawnController pawn)
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
        if (pawn.MovementInput.sqrMagnitude > 0.1f)
        {
            pawn.StateMachine.ChangeState(new WalkState(pawn));
        }

        if (Input.GetMouseButtonDown(0))
        {
            pawn.StateMachine.ChangeState(new ChopState(pawn));
        }
    }

    public void FixedUpdate() { }
}


