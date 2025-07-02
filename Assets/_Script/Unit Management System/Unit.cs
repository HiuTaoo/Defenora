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

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected CharacterMovement characterMovement;
    public Building assignedBuilding;


    // Events
    public System.Action<Unit> OnUnitDestroyed;
    public System.Action<Unit> OnDestinationReached;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterMovement = GetComponentInChildren<CharacterMovement>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f; // Top-down game không cần gravity
    }

    protected virtual void Update()
    {
        HandleMovement();
        UpdateAnimations();
    }

    // Di chuyển đến vị trí đích
    public virtual void MoveTo(Vector3 destination)
    {
        targetDestination = new GameObject($"{unitName}_Target").transform;
        targetDestination.position = destination;
        currentState = UnitState.Moving;
    }

    // Di chuyển đến một Transform cụ thể
    public virtual void MoveTo(Vector3Int position, int layer)
    {
        currentState = UnitState.Moving;

        characterMovement.MoveToPosition(position, layer);
    }


    // Dừng di chuyển
    public virtual void StopMovement()
    {
        targetDestination = null;
        rb.velocity = Vector2.zero;
        currentState = UnitState.Idle;
    }

    // Xử lý di chuyển
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

            // Xóa target tạm thời nếu có
            if (targetDestination.name.Contains("_Target"))
                Destroy(targetDestination.gameObject);
        }
        else
        {
            rb.velocity = direction * moveSpeed;
        }
    }

    // Cập nhật animations
    protected virtual void UpdateAnimations()
    {
        if (animator == null) return;

        /*animator.SetFloat("Speed", rb.velocity.magnitude);
        animator.SetBool("IsMoving", currentState == UnitState.Moving);*/

        // Flip sprite theo hướng di chuyển
        if (rb.velocity.x != 0)
        {
            spriteRenderer.flipX = rb.velocity.x < 0;
        }
    }

    // Nhận damage
    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
        {
            Die();
        }
    }

    // Hồi máu
    public virtual void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    // Chết
    protected virtual void Die()
    {
        OnUnitDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    // Khả năng đặc biệt của từng loại nhân vật
    public abstract void UseSpecialAbility();

    // Lấy thông tin nhân vật
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