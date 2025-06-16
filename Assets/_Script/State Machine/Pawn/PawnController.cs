using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PawnController : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public Rigidbody2D rb;
    public Animator animator;
    public AgentPhysics2D agentPhysics2D;
    public CircleCollider2D collider2D;

    public Vector2 MovementInput { get; private set; }

    public StateMachine StateMachine { get; private set; }

    private void Awake()
    {
        StateMachine = new StateMachine();
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        collider2D = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        HandleInput();
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    private void HandleInput()
    {
        MovementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    #region CHOP STATE
    public void EndChopAction()
    {
        if(StateMachine.CurrentState is ChopState chopState)
        {
            chopState.SetCompleted();
        }
    }
    #endregion

}
