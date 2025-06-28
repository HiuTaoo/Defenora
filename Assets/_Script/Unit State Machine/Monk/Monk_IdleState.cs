using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monk_IdleState : IUnitState
{
    MonkController monkController;

    public Monk_IdleState(MonkController controller)
    {
        this.monkController = controller;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        monkController.animator.Play("Idle");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        if (monkController.characterMovement.moving)
        {
            monkController.StateMachine.ChangeState(new Monk_WalkState(monkController));
        }

        if (monkController.MovementInput != Vector2.zero)
        {
            monkController.StateMachine.ChangeState(new Monk_WalkState(monkController));
        }
    }
}
