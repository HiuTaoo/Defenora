using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lancer_IdleState : IUnitState
{
    LancerController lancerController;

    public Lancer_IdleState(LancerController lancerController)
    {
        this.lancerController = lancerController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        lancerController.animator.Play("Idle");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        if (lancerController.characterMovement.moving)
        {
            lancerController.StateMachine.ChangeState(new Lancer_WalkState(lancerController));
        }

        if (lancerController.MovementInput != Vector2.zero)
        {
            lancerController.StateMachine.ChangeState(new Lancer_WalkState(lancerController));
        }
    }
}
