using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    private PawnController pawn;

    public WalkState(PawnController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Walk");
    }

    public void OnExit() { }

    public void Update()
    {
        if (pawn.MovementInput.sqrMagnitude < 0.1f)
        {
            pawn.StateMachine.ChangeState(new IdleState(pawn));
        }
        if (Input.GetMouseButtonDown(0))
        {
            pawn.StateMachine.ChangeState(new ChopState(pawn));
        }
    }

    public void FixedUpdate()
    {
        pawn.rb.velocity = pawn.MovementInput * pawn.moveSpeed;

        Vector3 velocity = pawn.rb.velocity;

        if (velocity.sqrMagnitude > 0.01f)
        {
            if (velocity.x < 0f)
            {
                Vector3 scale = pawn.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);  
                pawn.transform.localScale = scale;
            }
            else if (velocity.x > 0f)
            {
                Vector3 scale = pawn.transform.localScale;
                scale.x = Mathf.Abs(scale.x);   
                pawn.transform.localScale = scale;
            }
        }
    }

}
