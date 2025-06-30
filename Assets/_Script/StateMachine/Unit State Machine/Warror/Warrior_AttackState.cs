using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior_AttackState : MonoBehaviour
{
    WarriorController warriorController;

    public Warrior_AttackState(WarriorController warriorController)
    {
        this.warriorController = warriorController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        warriorController.animator.Play("Front 1");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
        
    }
}
