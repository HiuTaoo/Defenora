using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    /*public void FixedUpdate()
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
    }*/

    public void FixedUpdate()
    {
        Vector2 input = pawn.MovementInput;
        if (input.sqrMagnitude < 0.01f)
        {
            pawn.rb.velocity = Vector2.zero;
            return;
        }

        // Kiểm tra có vật cản phía trước không bằng AgentPhysics2D
        Vector2 currentPosition = pawn.rb.position;
        Vector2 direction = input.normalized;
        float moveDistance = pawn.moveSpeed * Time.fixedDeltaTime;


        // Raycast kiểm tra trước khi di chuyển
        bool isBlocked = pawn.agentPhysics2D.IsBlocked(currentPosition, direction, moveDistance + 0.05f, pawn.collider2D);

        if (!isBlocked)
        {
            Vector2 newPosition = currentPosition + direction * moveDistance;
            pawn.rb.MovePosition(newPosition);
        }
        else
        {
            pawn.rb.velocity = Vector2.zero;
        }

        // Flip hướng dựa theo input.x
        if (input.x < -0.1f)
        {
            Vector3 scale = pawn.transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            pawn.transform.localScale = scale;
        }
        else if (input.x > 0.1f)
        {
            Vector3 scale = pawn.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            pawn.transform.localScale = scale;
        }
    }

    


}
