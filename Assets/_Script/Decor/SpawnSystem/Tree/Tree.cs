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
    public Task currentTask;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    public CapsuleCollider2D treeCollider;

    private bool isBeingChopped = false;
    private bool hasBeenChopped = false;

    public System.Action<Tree> OnTreeChopped;

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
                animator.Play("Chopped");

            if (treeState == TreeState.Idle && currentChopHit == maxChopHit && !hasBeenChopped)
            {
                OnChoppedTree();
            }
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
        hasBeenChopped = true;
        treeState = TreeState.Chopped;
        animator.Play("Chopped");

        OnTreeChopped?.Invoke(this);

        var render = transform.Find("Custom Render Sprite");
        render.gameObject.SetActive(false);

        treeCollider.enabled = false;
        GraphNode.Instance.SetWalkableNode(positionInGrid,layerIndex, true); 
    }

}

public enum TreeState{
    Idle,
    Chopped
}