using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Windows;

public class Sheep : MonoBehaviour
{
    private CircleCollider2D collider2D;
    private Rigidbody2D rb;
    private Animator animator;
    private AgentPhysics2D agentPhysics2D;
    public FloorAgent floorAgent;

    [Header("Sheep Info")]
    public int layerIndex = 0; 
    public bool isDangerous = false;
    public int maxDuration = 15; 
    public float runSpeed = 5f;
    public float panicTime = 3f;
    public Vector2 runDirection;

    [Header("Sheep Settings")]
    public float alertDistance = 5f;

    private Coroutine panicCoroutine;
    private System.Random random;
    private Coroutine randomAnimationCoroutine;
    private Coroutine checkDangerCoroutine;

    private void Awake()
    {
        collider2D = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        random = new System.Random();

    }

    private void Start()
    {
        animator.Play("Idle");
        checkDangerCoroutine = StartCoroutine(CheckDangerLoop());
    }

    private void Update()
    {
        if (!isDangerous)
        {
            if (randomAnimationCoroutine == null)
            {
                randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
            }
        }
        layerIndex = floorAgent.currentFloorIndex;
        HandleFlipDirection();
    }

    private void FixedUpdate()
    {
        if (isDangerous)
        {
            if (randomAnimationCoroutine != null)
            {
                StopCoroutine(randomAnimationCoroutine);
                randomAnimationCoroutine = null;
            }

            if (panicCoroutine == null)
            {
                StartPanicRun();
            }

            PanicMove();
        }
        else
        {
            rb.velocity = Vector2.zero;

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
            {
                animator.Play("Idle");
            }

            if (randomAnimationCoroutine == null)
            {
                randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
            }
        }
    }

    #region Random Animation Loop
    private IEnumerator RandomAnimationLoop()
    {
        while (!isDangerous)
        {
            int waitTime = random.Next(3, maxDuration); 
            yield return new WaitForSeconds(waitTime);

            int nextState = random.Next(0, 2);
            switch (nextState)
            {
                case 0:
                    animator.Play("Idle");
                    break;
                case 1:
                    animator.Play("Eat");
                    break;
            }
        }

        randomAnimationCoroutine = null;
    }
    #endregion

    #region Panic Run
    public void StartPanicRun()
{
        if (panicCoroutine == null)
        {
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            panicCoroutine = StartCoroutine(PanicRunTimer());
        }
    }

    private IEnumerator PanicRunTimer()
    {
        animator.Play("Run");
        yield return new WaitForSeconds(panicTime);
        isDangerous = false;
        panicCoroutine = null;
    }


    private void PanicMove()
    {
        if (!isDangerous) return;

        Vector2 currentPosition = rb.position;
        float moveDistance = runSpeed * Time.fixedDeltaTime;

        bool isBlocked = agentPhysics2D.IsBlock(currentPosition, runDirection, moveDistance + 0.05f, collider2D);

        if (!isBlocked && GameLoop.Instance.StateMachine.CurrentStateType == GameStateType.Playing)
        {
            Vector2 newPosition = currentPosition + runDirection * moveDistance;
            rb.MovePosition(newPosition);
        }
        else
        {
            // Random lại hướng nếu bị kẹt
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        }
    }

    private void HandleFlipDirection()
    {
        if (runDirection.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (runDirection.x > 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    #endregion

    #region Check Dangerous Zone
    private IEnumerator CheckDangerLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(0.3f);

        while (true)
        {
            if (!isDangerous)
                CheckDangerousZone();

            yield return wait;
        }
    }

    private void CheckDangerousZone()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, alertDistance);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                isDangerous = true;
                return; 
            }
        }

    }

    #endregion


}
