using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior_WalkState : IUnitState
{
    WarriorController warriorController;

    public Warrior_WalkState(WarriorController warriorController)
    {
        this.warriorController = warriorController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        warriorController.animator.Play("Walk");
    }

    public void OnExit()
    {
        
    }

    public void Update()
    {
        if (!warriorController.characterMovement.moving && warriorController.MovementInput == Vector2.zero)
        {
            warriorController.StateMachine.ChangeState(new Warrior_IdleState(warriorController));
        }
    }
}
