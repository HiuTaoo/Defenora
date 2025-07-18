using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Animal : MonoBehaviour
{
    protected CircleCollider2D collider2D;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected AgentPhysics2D agentPhysics2D;
    public FloorAgent floorAgent;

    [Header("Animal Info")]
    public AnimalType animalType;
    public int layerIndex;
    public bool isDangerous = false;
    public Vector2 runDirection;

    [Header("Animal Settings")]
    public float alertDistance = 5f;
    public int maxDuration = 15;
    public float runSpeed = 5f;
    public float panicTime = 3f;

    protected Coroutine panicCoroutine;
    protected System.Random random;
    protected Coroutine randomAnimationCoroutine;
    protected Coroutine checkDangerCoroutine;

    protected virtual void Awake()
    {
        collider2D = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        agentPhysics2D = GetComponentInChildren<AgentPhysics2D>();
        floorAgent = GetComponentInChildren<FloorAgent>();
        random = new System.Random();
    }

    
    #region Random Animation Loop
    protected virtual IEnumerator RandomAnimationLoop()
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
    public virtual void StartPanicRun()
    {
        if (panicCoroutine == null)
        {
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            panicCoroutine = StartCoroutine(PanicRunTimer());
        }
    }

    protected virtual IEnumerator PanicRunTimer()
    {
        animator.Play("Run");
        yield return new WaitForSeconds(panicTime);
        isDangerous = false;
        panicCoroutine = null;
    }

    protected virtual void PanicMove()
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
            float angle = Random.Range(0f, 360f);
            runDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        }
    }

    protected virtual void HandleFlipDirection()
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
    protected virtual IEnumerator CheckDangerLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(0.3f);

        while (true)
        {
            if (!isDangerous)
                CheckDangerousZone();

            yield return wait;
        }
    }

    protected virtual void CheckDangerousZone()
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


public enum AnimalType
{
    Sheep
}

