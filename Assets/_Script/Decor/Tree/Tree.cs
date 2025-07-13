using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree : MonoBehaviour
{
    [Header("Tree Info")]
    public TreeState treeState = TreeState.Idle;
    public int maxChopHit = 5;
    public int currentLayerIndex = 0;
    public int currentChopHit = 0;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isBeingChopped = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

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
    }

}

public enum TreeState{
    Idle,
    Chopped
}