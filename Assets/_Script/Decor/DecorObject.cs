using System;
using System.Collections;
using UnityEngine;

public abstract class DecorObject : MonoBehaviour, IChoppable, IPoolable
{
    public int maxChopHit = 5;
    public int layerIndex;
    public int currentChopHit = 0;

    private bool isBeingCleared = false;
    private bool hasBeenChopped = false;

    private CapsuleCollider2D decorCollider;
    public SpriteRenderer spriteRenderer;
    private SimpleSpriteAnimator spriteAnimator;

    public Action<IChoppable> OnChoppedObject { get; set; }
    
    private Builder claimedBy;

    public bool IsClaimed => claimedBy != null;

    public virtual void Awake()
    {
        decorCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = GetComponent<SimpleSpriteAnimator>();
    }

    private void Update()
    {
        if (currentChopHit == maxChopHit && !hasBeenChopped)
        {
            OnChopped();
        }
    }

    public void HandleChopped()
    {
        if (currentChopHit >= maxChopHit) return;

        currentChopHit++;

        if (!isBeingCleared)
        {
            StartCoroutine(ClearObjectEffect());
        }
    }

    private IEnumerator ClearObjectEffect()
    {
        isBeingCleared = true;

        spriteRenderer.color = new Color32(207, 207, 207, 255);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;

        isBeingCleared = false;
    }

    public void OnClearObject()
    {
        hasBeenChopped = true;
        Release(claimedBy);
        PoolManager.Instance.Despawn(transform.gameObject);
        Debug.Log("Despawn Obstacle");
        OnChoppedObject?.Invoke(this); 
    }

    public virtual void OnChopped()
    {
        OnClearObject();
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

    /// <summary>
    /// Được gọi ngay khi Decor Object được hồi sinh từ Pool ra Bản đồ
    /// </summary>
    public virtual void OnSpawned()
    {
        if (decorCollider == null) decorCollider = GetComponent<CapsuleCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteAnimator == null) spriteAnimator = GetComponent<SimpleSpriteAnimator>();

        currentChopHit = 0;
        isBeingCleared = false;
        hasBeenChopped = false;

        claimedBy = null;
        OnChoppedObject = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = true; 
        }

        if (spriteAnimator != null)
        {
            spriteAnimator.enabled = true;
            spriteAnimator.Play();
        }

        if (decorCollider != null)
        {
            decorCollider.enabled = true;
        }

        var render = transform.Find("Custom Render Sprite");
        if (render != null)
        {
            render.gameObject.SetActive(true);
        }

        RegionManager.Instance.RegisterObject(gameObject);
    }

    /// <summary>
    /// Được gọi ngay trước khi Decor Object bị giấu ngầm vào bên trong Pool
    /// </summary>
    public virtual void OnDespawned()
    {
        claimedBy = null;
        OnChoppedObject = null;

        if (spriteAnimator != null)
        {
            spriteAnimator.Stop();
            spriteAnimator.enabled = false;
        }

        RegionManager.Instance.UnregisterObject(gameObject);
    }
}