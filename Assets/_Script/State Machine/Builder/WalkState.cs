using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using static UnityEngine.RuleTile.TilingRuleOutput;

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
        if (!pawn.characterMovement.moving && pawn.MovementInput == Vector2.zero)
        {
            pawn.StateMachine.ChangeState(new IdleState(pawn));
        }
        /*        if (Input.GetMouseButtonDown(0))
                {
                    pawn.StateMachine.ChangeState(new ChopState(pawn));
                }*/
    }

    public void FixedUpdate()
    {
        Move();
        HandleDirection();
    }

    public void Move()
    {
        Vector2 input = pawn.MovementInput;

        if (input.sqrMagnitude < 0.01f)
        {
            pawn.rb.velocity = Vector2.zero;
            return;
        }

        Vector2 currentPosition = pawn.rb.position;
        Vector2 direction = input.normalized;
        float moveDistance = pawn.moveSpeed * Time.fixedDeltaTime;

        bool isBlocked = pawn.agentPhysics2D.IsBlock(currentPosition, direction, moveDistance + 0.05f, pawn.collider2D);

        if (!isBlocked)
        {
            Vector2 newPosition = currentPosition + direction * moveDistance;
            pawn.rb.MovePosition(newPosition);
        }
        else
        {
            pawn.rb.velocity = Vector2.zero;
        }
    }

    public void HandleDirection()
    {
        //pawn.rb.velocity = pawn.MovementInput * pawn.moveSpeed;

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
