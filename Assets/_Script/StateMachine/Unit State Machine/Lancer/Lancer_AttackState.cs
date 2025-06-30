using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lancer_AttackState : IUnitState
{
    LancerController lancerController;

    public Lancer_AttackState(LancerController lancerController)
    {
        this.lancerController = lancerController;
    }

    public void FixedUpdate()
    {
    }

    public void OnEnter()
    {
        lancerController.animator.Play("Right Defend");
    }

    public void OnExit()
    {

    }

    public void Update()
    {
    }
}
