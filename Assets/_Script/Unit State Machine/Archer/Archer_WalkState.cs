using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer_WalkState : IUnitState
{
    private ArcherController archerController;

    public Archer_WalkState(ArcherController controller)
    {
        this.archerController = controller;
    }

    public void FixedUpdate()
    {
       
    }

    public void OnEnter()
    {
        archerController.animator.Play("Run");
        archerController.rb.velocity = Vector2.zero;
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        if (!archerController.characterMovement.moving && archerController.MovementInput == Vector2.zero)
        {
            archerController.StateMachine.ChangeState(new Archer_IdleState(archerController));
        }
    }
}
