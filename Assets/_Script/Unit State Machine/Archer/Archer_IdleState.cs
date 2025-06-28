using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer_IdleState : IUnitState
{
    private ArcherController archerController;

    public Archer_IdleState(ArcherController controller) 
    {
        this.archerController = controller;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        archerController.animator.Play("Idle");
        archerController.rb.velocity = Vector2.zero;
    }

    public void OnExit()
    {
        
    }

    public void Update()
    {
        if (archerController.characterMovement.moving)
        {
            archerController.StateMachine.ChangeState(new Archer_WalkState(archerController));
        }

        if (archerController.MovementInput != Vector2.zero)
        {
            archerController.StateMachine.ChangeState(new Archer_WalkState(archerController));
        }
    }
}
