using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_CarryState : IUnitState
{
    private BuilderController pawn;

    public Builder_CarryState(BuilderController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Carry");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() { }

    public void Update()
    {
    }

    public void FixedUpdate() { }
}
