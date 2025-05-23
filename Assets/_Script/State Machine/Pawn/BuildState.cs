using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildState : IState
{
    private PawnController pawn;

    public BuildState(PawnController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Build");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() { }

    public void Update()
    {
        if (false)
        {
            pawn.StateMachine.ChangeState(new IdleState(pawn));
        }
    }

    public void FixedUpdate() { }
}
