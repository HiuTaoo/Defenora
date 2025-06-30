using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior_IdleState : IUnitState
{
    WarriorController warriorController;

    public Warrior_IdleState(WarriorController warriorController)
    {
        this.warriorController = warriorController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        warriorController.animator.Play("Idle");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        if (warriorController.characterMovement.moving)
        {
            warriorController.StateMachine.ChangeState(new Warrior_WalkState(warriorController));
        }

        if (warriorController.MovementInput != Vector2.zero)
        {
            warriorController.StateMachine.ChangeState(new Warrior_WalkState(warriorController));
        }
    }
}
