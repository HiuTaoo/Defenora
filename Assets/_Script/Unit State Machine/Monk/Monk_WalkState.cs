using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monk_WalkState : IUnitState
{
    MonkController monkController;

    public Monk_WalkState(MonkController controller)
    {
        this.monkController = controller;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        monkController.animator.Play("Walk");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        if (!monkController.characterMovement.moving && monkController.MovementInput == Vector2.zero)
        {
            monkController.StateMachine.ChangeState(new Monk_IdleState(monkController));
        }
    }
}
