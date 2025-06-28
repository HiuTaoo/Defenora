using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monk_HealState : IUnitState
{
    MonkController monkController;

    public Monk_HealState(MonkController controller)
    {
        this.monkController = controller;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        monkController.animator.Play("Heal");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
    }
}
