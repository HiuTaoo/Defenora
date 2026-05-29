using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Builder Info")]
    public float moveSpeed = 3.5f;

    public Rigidbody2D rb;
    public Animator animator;
    public AgentPhysics2D agentPhysics2D;
    public CircleCollider2D builderCollider;
    public CharacterMovement characterMovement;

    public Vector2 MovementInput { get; private set; }


    private void Awake()
    {
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        builderCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        Move();
        HandleDirection();
    }

    private void HandleInput()
    {
        MovementInput = GameManager.Instance.gameContext.InputManager.GetMovementInput();
    }

    public int GetCurrentLayerIndex()
    {
        var flootAgent = gameObject.GetComponentInChildren<FloorAgent>();
        return flootAgent != null ? flootAgent.currentFloorIndex : 0;
    }

    public void Move()
    {
        Vector2 input = MovementInput;

        if (input.sqrMagnitude < 0.01f)
        {
            rb.velocity = Vector2.zero;
            animator.Play("Idle");
            return;
        }

        Vector2 currentPosition = rb.position;
        Vector2 direction = input.normalized;
        float moveDistance = moveSpeed * Time.fixedDeltaTime;

        bool isBlocked = agentPhysics2D.IsBlock(currentPosition, direction, moveDistance + 0.05f, GetComponent<CircleCollider2D>());

        if (!isBlocked && GameManager.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            animator.Play("Walk");
            Vector2 newPosition = currentPosition + direction * moveDistance;
            rb.MovePosition(newPosition);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void HandleDirection()
    {
        Vector2 velocity = MovementInput * moveSpeed;

        if (velocity.sqrMagnitude > 0.01f)
        {
            if (velocity.x < 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else if (velocity.x > 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
    
}
