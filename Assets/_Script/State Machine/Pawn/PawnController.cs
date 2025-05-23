using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PawnController : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public Rigidbody2D rb;
    public Animator animator;

    public Vector2 MovementInput { get; private set; }

    public StateMachine StateMachine { get; private set; }

    private void Awake()
    {
        StateMachine = new StateMachine();
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
