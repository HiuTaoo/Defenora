using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lancer_WalkState : IUnitState
{
    LancerController lancerController;

    public Lancer_WalkState(LancerController lancerController)
    {
        this.lancerController = lancerController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        lancerController.animator.Play("Walk");
    }

    public void OnExit()
    {

    }

    public void Update()
    {

        if (!lancerController.characterMovement.moving && lancerController.MovementInput == Vector2.zero)
        {
            lancerController.StateMachine.ChangeState(new Lancer_IdleState(lancerController));
        }
    }
}
