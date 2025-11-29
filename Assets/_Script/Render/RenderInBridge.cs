using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RenderInBridge : MonoBehaviour
{
    private TilemapRenderer  tilemapRenderer;

    private void Awake()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        
        DynamicSortingYX sorting = other.GetComponent<DynamicSortingYX>();
        if (sorting != null && sorting)
            sorting.enabled = false;
        
        SpriteRenderer spriteRenderer = other.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = tilemapRenderer.sortingOrder++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        
        DynamicSortingYX sorting = other.GetComponent<DynamicSortingYX>();
        if (sorting != null && !sorting.enabled)
            sorting.enabled = true;
    }
}
