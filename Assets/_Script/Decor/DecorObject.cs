using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DecorObject : MonoBehaviour, IChoppable
{
    public int maxChopHit = 5;
    public int layerIndex;
    public int currentChopHit = 0;

    private bool isBeingCleared = false;
    private bool hasBeenChopped = false;

    private CapsuleCollider2D decorCollider;
    private SpriteRenderer spriteRenderer;

    public Action<IChoppable> OnChoppedObject { get; set; }
    
    private Builder claimedBy;

    public bool IsClaimed => claimedBy != null;

    public virtual void Awake()
    {
        decorCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        int prefabIndex = -1;

        if (gameObject.CompareTag("Bush"))
        {
            Release(claimedBy);
            prefabIndex = SaveLoadSystem.Instance.GetPrefabIndex(gameObject, ObjectSpawner.Instance.spawnSettings.bushPrefabs);
            SaveLoadSystem.Instance.ReturnToPool(gameObject, ObjectSpawner.Instance.spawnSettings.bushPrefabs[prefabIndex]);
        }
        else if (gameObject.CompareTag("Rock"))
        {
            Release(claimedBy);
            prefabIndex = SaveLoadSystem.Instance.GetPrefabIndex(gameObject, ObjectSpawner.Instance.spawnSettings.rockPrefabs);
            SaveLoadSystem.Instance.ReturnToPool(gameObject, ObjectSpawner.Instance.spawnSettings.rockPrefabs[prefabIndex]);
        }

        gameObject.SetActive(false);
        OnChoppedObject?.Invoke(this); 
    }

    public void OnChopped()
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
}
