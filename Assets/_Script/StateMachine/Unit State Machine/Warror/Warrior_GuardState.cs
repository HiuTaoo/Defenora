using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior_GuardState : MonoBehaviour
{
    WarriorController warriorController;

    public Warrior_GuardState(WarriorController warriorController)
    {
        this.warriorController = warriorController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        warriorController.animator.Play("Guard");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        
    }
}
