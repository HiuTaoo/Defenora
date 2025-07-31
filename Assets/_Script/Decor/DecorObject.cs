using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DecorObject : MonoBehaviour, IChoppable
{
    public int maxChopHit = 2;
    public int layerIndex;
    public int currentChopHit = 0;

    private bool isBeingCleared = false;
    private bool hasBeenChopped = false;

    private CapsuleCollider2D decorCollider;
    private SpriteRenderer spriteRenderer;

    public Action<IChoppable> OnChoppedObject { get; set; }

    private void Awake()
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

    private void OnClearObject()
    {
        hasBeenChopped = true;
        int prefabIndex = -1;

        if (gameObject.CompareTag("Bush"))
        {
            prefabIndex = SaveLoadSystem.Instance.GetPrefabIndex(gameObject, ObjectSpawner.Instance.spawnSettings.bushPrefabs);
            SaveLoadSystem.Instance.ReturnToPool(ObjectSpawner.Instance.spawnSettings.bushPrefabs[prefabIndex], gameObject);
        }
        else if (gameObject.CompareTag("Rock"))
        {
            prefabIndex = SaveLoadSystem.Instance.GetPrefabIndex(gameObject, ObjectSpawner.Instance.spawnSettings.rockPrefabs);
            SaveLoadSystem.Instance.ReturnToPool(ObjectSpawner.Instance.spawnSettings.rockPrefabs[prefabIndex], gameObject);
        }

        gameObject.SetActive(false);
        OnChoppedObject?.Invoke(this); 
    }

    public void OnChopped()
    {
        OnClearObject();
    }

}
