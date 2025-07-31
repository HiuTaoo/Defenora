using System.Collections;
using UnityEngine;

public class Builder_ChopState : IUnitState
{
    private BuilderController pawn;
    public IChoppable currentTarget;
    private GameObject currentTargetGameObject;
    private bool isCompleted = false;
    private float chopCooldown = 0.5f;

    private Vector2 facingDir;
    private Vector2 origin;

    private Coroutine cooldownCoroutine;

    public Builder_ChopState(BuilderController pawn, GameObject currentTargetGameObject)
    {
        this.pawn = pawn;
        this.currentTargetGameObject = currentTargetGameObject;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Chop");
        pawn.rb.velocity = Vector2.zero;

        currentTarget = currentTargetGameObject.GetComponent<IChoppable>();

        if (currentTarget != null)
        {
            currentTarget.OnChoppedObject += HandleCompleteChop;
        }
    }

    public void OnExit()
    {
        if (currentTarget != null)
        {
            currentTarget.OnChoppedObject -= HandleCompleteChop;
        }

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

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, pawn.chopBoxSize, 0f, layer);

        currentTarget = null;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tree") && hit.gameObject == pawn.builderUnit.currentTask.targetGameObject)
            {
                currentTarget = hit.gameObject.GetComponent<Tree>();
                break;
            }
            /*else if (hit.CompareTag("Bush") && hit.gameObject == pawn.builderUnit.currentTask.targetGameObject)
            {
                currentTarget = hit.gameObject.GetComponent<Bush>();
                break;
            }*/
            // Thêm điều kiện cho các đối tượng khác như Rock, Building, v.v.
        }
    }

    public void StartCooldown()
    {
        pawn.animator.Play("Idle");
        cooldownCoroutine = pawn.StartCoroutine(ChopCooldownCoroutine());
    }

    private IEnumerator ChopCooldownCoroutine()
    {
        yield return new WaitForSeconds(chopCooldown);
        pawn.animator.Play("Chop");
    }

    private void HandleCompleteChop(IChoppable choppedObject)
    {
        if (choppedObject == null) return;

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
        currentTarget = null;
    }

    public void DrawGizmos()
    {
        if (pawn == null) return;

        facingDir = pawn.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, pawn.chopBoxSize);
    }
}
