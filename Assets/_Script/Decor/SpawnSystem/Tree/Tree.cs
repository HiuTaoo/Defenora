using System;
using System.Collections;
using System.Collections.Generic;
using _Script.BT.BlackBoard;
using UnityEngine;

public class Tree : MonoBehaviour, IChoppable, IPoolable
{
    [Header("Tree Info")]
    public TreeState treeState = TreeState.Idle;
    public int maxChopHit = 5;
    public int layerIndex;
    public int currentChopHit = 0;
    public Vector3Int positionInGrid;
    public Task currentTask;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    public CapsuleCollider2D treeCollider;

    private bool isBeingChopped = false;
    private bool hasBeenChopped = false;

    public Action<IChoppable> OnChoppedObject { get; set; } 
    
    private Builder claimedBy;

    public bool IsClaimed => claimedBy != null;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        treeCollider = GetComponent<CapsuleCollider2D>();

        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        RegisterState();
    }

    private void RegisterState()
    {
        treeState = TreeState.Idle;
        animator.Play("idle");
    }

    private void Update()
    {
        if (spriteRenderer.isVisible)
        {
            if (treeState == TreeState.Chopped)
            {
                animator.Play("Chopped");
            }
            
            if (treeState == TreeState.Idle && currentChopHit == maxChopHit && !hasBeenChopped)
            {
                OnChopped();
            }
        }
    }

    public void HandleChopped()
    {
        if (currentChopHit >= maxChopHit) return;

        currentChopHit++;

        if (!isBeingChopped)
        {
            StartCoroutine(ChopTreeEffect());
        }
    }

    private IEnumerator ChopTreeEffect()
    {
        isBeingChopped = true;

        spriteRenderer.color = new Color32(207, 207, 207, 255);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;

        isBeingChopped = false;
    }

    public void OnChopped()
    {
        hasBeenChopped = true;
        treeState = TreeState.Chopped;
        animator.Play("Chopped");

        // GraphNode.Instance.SetWalkableNode(positionInGrid, layerIndex, true);

        var render = transform.Find("Custom Render Sprite");
        render.gameObject.SetActive(false);

        // treeCollider.enabled = false;

        OnChoppedObject?.Invoke(this);
    }

    public bool TryClaim(Builder builder)
    {
        if (claimedBy != null)
            return false;

        claimedBy = builder;
        return true;
    }

    public void Release(Builder builder)
    {
        if (claimedBy == builder)
            claimedBy = null;
    }
    
    public Task GetTask()
    {
        return currentTask;
    }

    public void SetTask(Task task)
    {
        currentTask = task;
    }

    /// <summary>
    /// Được gọi ngay khi Cây được lấy ra từ PoolManager để đặt lên bản đồ
    /// </summary>
    public void OnSpawned()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (treeCollider == null) treeCollider = GetComponent<CapsuleCollider2D>();

        currentChopHit = 0;
        isBeingChopped = false;
        hasBeenChopped = false;
        treeState = TreeState.Idle;

        claimedBy = null;
        currentTask = null;
        OnChoppedObject = null; 

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = true; 
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("idle", 0, 0f); 
        }

        if (treeCollider != null)
        {
            treeCollider.enabled = true;
        }

        var render = transform.Find("Custom Render Sprite");
        if (render != null)
        {
            render.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Được gọi ngay trước khi Cây bị thu hồi về ngầm trong PoolManager
    /// </summary>
    public void OnDespawned()
    {
        claimedBy = null;
        currentTask = null;
        OnChoppedObject = null;

        if (animator != null)
        {
            animator.enabled = false;
        }
    }
}

public enum TreeState
{
    Idle,
    Chopped
}
