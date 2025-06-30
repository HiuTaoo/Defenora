using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_BuildState : IUnitState
{
    private BuilderController pawn;

    public Builder_BuildState(BuilderController pawn)
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

    }

    public void FixedUpdate() { }
}
