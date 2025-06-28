using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer_ShootState : IUnitState
{
    private ArcherController archerController;

    public Archer_ShootState(ArcherController controller)
    {
        this.archerController = controller;
    }

    public void FixedUpdate()
    {
        throw new System.NotImplementedException();
    }

    public void OnEnter()
    {
        archerController.animator.Play("Shoot Front");
        archerController.rb.velocity = Vector2.zero;
    }

    public void OnExit()
    {

    }

    public void Update()
    {

    }
}
