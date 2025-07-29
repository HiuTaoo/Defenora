using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_ChopState : IUnitState
{
    private BuilderController pawn;
    public Tree currentTree;
    private bool isCompleted = false;

    private float chopCooldown = 0.5f;
    private bool canChop = true;

    private Vector2 facingDir;
    private Vector2 origin;

    private Coroutine cooldownCoroutine;

    public Builder_ChopState(BuilderController pawn, Tree tree)
    {
        this.pawn = pawn;
        currentTree = tree;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Chop");
        pawn.rb.velocity = Vector2.zero;

        currentTree.OnTreeChopped -= HandleCompleteChopTree;
        currentTree.OnTreeChopped += HandleCompleteChopTree;
    }

    public void OnExit()
    {
        if (currentTree != null)
            currentTree.OnTreeChopped -= HandleCompleteChopTree;

        if (cooldownCoroutine != null)
            pawn.StopCoroutine(cooldownCoroutine);
    }

    public void Update()
    {
        if (isCompleted)
        {
            pawn.StateMachine.ChangeState(new Builder_IdleState(pawn));
            isCompleted = false;
            return;
        }

        if (canChop)
            TryChop();
    }

    public void FixedUpdate() { }

    public void SetCompleted()
    {
        isCompleted = true;
    }

    private void TryChop()
    {
        facingDir = pawn.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        int layer = LayerMask.GetMask($"Layer {pawn.GetCurrentLayerIndex() + 1}");
        Collider2D hit = Physics2D.OverlapBox(origin, pawn.chopBoxSize, 0f, layer);

        if (hit != null && hit.CompareTag("Tree"))
        {
            currentTree = hit.GetComponent<Tree>();
        }
        else
        {
            currentTree = null;
        }
    }

    public void StartCooldown()
    {
        canChop = false;
        pawn.animator.Play("Idle");
        cooldownCoroutine = pawn.StartCoroutine(ChopCooldownCoroutine());
    }

    private IEnumerator ChopCooldownCoroutine()
    {
        yield return new WaitForSeconds(chopCooldown);
        canChop = true;
        pawn.animator.Play("Chop");
    }

    private void HandleCompleteChopTree(Tree tree)
    {
        if (tree == null) return;

        tree.OnTreeChopped -= HandleCompleteChopTree;
        SetCompleted();

        Task completedTask = pawn.builderUnit.currentTask;

        if (completedTask != null)
        {
            completedTask.taskStatus = TaskStatus.Completed;
            TaskManager.Instance.CompletedTask(completedTask);
        }

        pawn.builderUnit.currentState = UnitState.Idle;
        pawn.builderUnit.currentTask = null;

        pawn.builderUnit.OnUnitIdle?.Invoke(pawn.builderUnit);
        currentTree = null;

        Debug.Log($"Builder {pawn.builderUnit.unitName} đang hoàn thành task chặt cây và giờ đang rảnh.");
    }

    public void DrawGizmos()
    {
        if (pawn == null) return;

        Vector2 facingDir = pawn.transform.right.normalized * pawn.transform.localScale.x;
        Vector2 origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, pawn.chopBoxSize);
    }
}
