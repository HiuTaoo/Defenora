using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Tree : MonoBehaviour
{
    [Header("Tree Info")]
    public TreeState treeState = TreeState.Idle;
    public int maxChopHit = 5;
    public int layerIndex;
    public int currentChopHit = 0;
    public Vector3Int positionInGrid;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D collider2D;

    private bool isBeingChopped = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        collider2D = GetComponent<CapsuleCollider2D>();

        RegisterState();
    }

    private void RegisterState()
    {
        treeState = TreeState.Idle;
        animator.Play("idle");  
    }

    private void Update()
    {
        if (treeState == TreeState.Idle && currentChopHit == maxChopHit)
        {
            OnChoppedTree();
        }

        if(treeState == TreeState.Chopped)
        {
            animator.Play("Chopped");
        }
        if (treeState == TreeState.Idle)
        {
            animator.Play("idle");
        }

    }

    public void HandleChopTree()
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

    private void OnChoppedTree()
    {
        treeState = TreeState.Chopped;
        animator.Play("Chopped");

        var render = transform.Find("Custom Render Sprite");
        render.gameObject.SetActive(false);

        collider2D.enabled = false;
        GraphNode.Instance.SetWalkableNode(positionInGrid,layerIndex, true); 
    }

}

public enum TreeState{
    Idle,
    Chopped
}