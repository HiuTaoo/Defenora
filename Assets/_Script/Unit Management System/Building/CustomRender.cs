using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomRender : MonoBehaviour
{
    private BoxCollider2D checkingCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        checkingCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.GetComponent<SpriteRenderer>() != null)
        {
            collision.gameObject.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.GetComponent<SpriteRenderer>() != null)
        {
            collision.gameObject.GetComponentInChildren<FloorAgent>().UpdateVisualElements();
        }
    }
}
