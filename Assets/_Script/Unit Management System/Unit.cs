using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public UnitType unitType;
    public UnitState currentState = UnitState.Idle;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float attackDamage = 10f;
    public float attackRange = 2f;

    [Header("Movement")]
    public Transform targetDestination;
    public float stoppingDistance = 0.1f;

    [Header("Task")]
    public Task currentTask = null;

    protected Rigidbody2D rb;
    protected Animator animator;
    public SpriteRenderer spriteRenderer;
    public CharacterMovement characterMovement;
    public Building assignedBuilding;
    public FloorAgent floorAgent;

    public System.Action<Unit> OnUnitDestroyed;
    public System.Action<Unit> OnDestinationReached;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterMovement = GetComponentInChildren<CharacterMovement>();
        floorAgent = GetComponentInChildren<FloorAgent>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        unitName = gameObject.name;
    }

    protected virtual void Update()
    {
        UpdateAnimations();
        if(currentTask.targetGameObject != null && currentTask.taskStatus == TaskStatus.NotStarted)
        {
            StartCoroutine(ExecuteTask(currentTask));
        }
    }

    public virtual void MoveToTaskPosition(Vector3Int position, int layer)
    {
        currentState = UnitState.Moving;
        characterMovement.MoveToTaskPosition(position, layer);
    }

    public IEnumerator ExecuteTask(Task task)
    {
        yield return new WaitForSeconds(0.1f);

        task.taskStatus = TaskStatus.InProgress;
        targetDestination = task.targetGameObject.transform;
        MoveToTaskPosition(Vector3Int.FloorToInt(task.targetGameObject.transform.position), task.layerIndex);
    }

    public virtual void StopMovement()
    {
        targetDestination = null;
        rb.velocity = Vector2.zero;
        currentState = UnitState.Idle;
    }

    protected virtual void HandleMovement()
    {
        if (targetDestination == null || currentState != UnitState.Moving)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector3 direction = (targetDestination.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetDestination.position);

        if (distance <= stoppingDistance)
        {
            rb.velocity = Vector2.zero;
            currentState = UnitState.Stationed;
            OnDestinationReached?.Invoke(this);

            if (targetDestination.name.Contains("_Target"))
                Destroy(targetDestination.gameObject);
        }
        else
        {
            rb.velocity = direction * moveSpeed;
        }
    }

    protected virtual void UpdateAnimations()
    {
        if (animator == null) return;

        if (rb.velocity.x != 0)
        {
            spriteRenderer.flipX = rb.velocity.x < 0;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    protected virtual void Die()
    {
        OnUnitDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public abstract void UseSpecialAbility();

    public virtual UnitData GetUnitInfo()
    {
        return new UnitData
        {
            unitName = this.unitName,
            unitType = this.unitType,
            currentState = this.currentState,
            health = this.health,
            maxHealth = this.maxHealth,
            position = transform.position
        };
    }
}