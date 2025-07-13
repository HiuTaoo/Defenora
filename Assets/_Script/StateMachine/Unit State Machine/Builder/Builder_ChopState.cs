using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_ChopState : IUnitState
{
    private BuilderController pawn;
    public Tree currentTree;
    private bool isCompleted = false;

    public Builder_ChopState(BuilderController pawn)
    {
        this.pawn = pawn;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Chop");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() { }

    public void Update()
    {
        if (isCompleted)
        {
            pawn.StateMachine.ChangeState(new Builder_IdleState(pawn));
            isCompleted = false;
            return;
        }

        TryChop();
    }

    public void FixedUpdate() { }

    public void SetCompleted()
    {
        isCompleted = true;
    }

    private void TryChop()
    {
        Vector2 facingDir = pawn.transform.right.normalized * pawn.transform.localScale.x;

        Vector2 origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        Collider2D hit = Physics2D.OverlapBox(origin, pawn.chopBoxSize, 0f, LayerMask.GetMask("Decor"));

        if (hit != null)
        {
            currentTree = hit.GetComponent<Tree>();

        }
        else
        {
            currentTree = null;
        }
    }

    public void DrawGizmos()
    {
        if (pawn == null) return;

        Vector2 facingDir = pawn.transform.right.normalized * pawn.transform.localScale.x;
        Vector2 origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, pawn.chopBoxSize);
    }

}
