using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class BuilderController : MonoBehaviour
{
    [Header("Builder Info")]
    public float moveSpeed = 3.5f;
    public Vector2 chopBoxSize = new Vector2(1f, 1f);
    public float chopDistance = 1f;

    public Rigidbody2D rb;
    public Animator animator;
    public AgentPhysics2D agentPhysics2D;
    public CircleCollider2D builderCollider;
    public CharacterMovement characterMovement;
    public FloorAgent floorAgent;
    public Unit builderUnit;

    public Vector2 MovementInput { get; private set; }

    public UnitStateMachine StateMachine { get; private set; }

    private void Awake()
    {
        StateMachine = new UnitStateMachine();
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        builderCollider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        builderUnit = GetComponent<Unit>();
    }

    private void Start()
    {
        StateMachine.ChangeState(new Builder_IdleState(this));
    }

    private void Update()
    {
        //HandleInput();
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }
/*
    private void HandleInput()
    {
        //MovementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        MovementInput = GameLoop.Instance.gameContext.InputManager.GetMovementInput();
    }*/

    public int GetCurrentLayerIndex()
    {
        return floorAgent != null ? floorAgent.currentFloorIndex : 0;
    }

    #region CHOP STATE
    public void EndChopAction()
    {
        if (StateMachine.CurrentState is Builder_ChopState chopState)
        {
            chopState.StartCooldown();
        }
    }

    public void Chop()
    {
        if (StateMachine.CurrentState is Builder_ChopState chopState)
        {
            if (chopState.currentTree != null)
            {
                chopState.currentTree.HandleChopTree();
            }
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (StateMachine.CurrentState is Builder_ChopState chopState)
        {
            chopState.DrawGizmos();
        }
    }
}
