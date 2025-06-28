using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherController : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator animator;
    public AgentPhysics2D agentPhysics2D;
    public CircleCollider2D archerCollider;
    public CharacterMovement characterMovement;

    public Vector2 MovementInput { get; private set; }

    public UnitStateMachine StateMachine { get; private set; }

    private void Awake()
    {
        StateMachine = new UnitStateMachine();

        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        archerCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
    }

    private void Start()
    {
        StateMachine.ChangeState(new Archer_IdleState (this));
    }

    private void Update()
    {
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }


   
}
