using UnityEngine;

public class Builder_WalkState : IUnitState
{
    private BuilderController pawn;

    public Builder_WalkState(BuilderController pawn)
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
            pawn.StateMachine.ChangeState(new Builder_IdleState(pawn));
        }
        if (Input.GetMouseButtonDown(0))
        {
            pawn.StateMachine.ChangeState(new Builder_ChopState(pawn));
        }
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

        bool isBlocked = pawn.agentPhysics2D.IsBlock(currentPosition, direction, moveDistance + 0.05f, pawn.GetComponent<CircleCollider2D>());

        if (!isBlocked && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
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
        Vector2 velocity = pawn.MovementInput * pawn.moveSpeed;

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
