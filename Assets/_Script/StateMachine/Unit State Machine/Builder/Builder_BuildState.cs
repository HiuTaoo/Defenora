using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_BuildState : IUnitState
{
    private BuilderController pawn;
    public IBuildable currentBuilding;
    public GameObject currentBuildingGameObject;

    private bool isCompleted = false;
    private float buildCooldown = 0.5f;

    private Vector2 facingDir;
    private Vector2 origin;

    private Coroutine cooldownCoroutine;

    public Builder_BuildState(BuilderController pawn, GameObject building)
    {
        this.pawn = pawn;
        currentBuildingGameObject = building;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Build");
        pawn.rb.velocity = Vector2.zero;

        currentBuilding = currentBuildingGameObject.GetComponent<IBuildable>();
        if (currentBuilding != null)
        {
            currentBuilding.OnBuiltObject += HandleCompleteBuild;
        }
    }

    public void OnExit() {
        if (currentBuilding != null)
        {
            currentBuilding.OnBuiltObject -= HandleCompleteBuild;
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
    }

    public void FixedUpdate() {
        TryBuild();
    }

    public void SetCompleted()
    {
        isCompleted = true;
    }

    private void TryBuild()
    {
        if(currentBuilding == null || currentBuildingGameObject == null)
            return;

        facingDir = pawn.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        int layer = LayerMask.GetMask($"Building");

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, pawn.chopBoxSize, 0f, layer);

        currentBuilding = null;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Building") && hit.gameObject == pawn.builderUnit.currentTask.targetGameObject)
            {
                currentBuilding = hit.gameObject.GetComponent<IBuildable>();
                break;
            }
        }

    }


    public void StartCooldown()
    {
        pawn.animator.Play("Idle");
        cooldownCoroutine = pawn.StartCoroutine(BuildCooldownCoroutine());

    }

    private IEnumerator BuildCooldownCoroutine()
    {
        yield return new WaitForSeconds(buildCooldown);
        pawn.animator.Play("Build");
    }

    private void HandleCompleteBuild(IBuildable buildableObject)
    {
        if (buildableObject == null) return;

        SetCompleted();
        var building = buildableObject as Building;
        building.currentTask = null;

        Task completedTask = pawn.builderUnit.currentTask;

        if (completedTask != null)
        {
            completedTask.taskStatus = TaskStatus.Completed;
            TaskManager.Instance.CompletedTask(completedTask);
            pawn.builderUnit.currentTask = null;
        }
        pawn.builderUnit.currentState = UnitState.Idle;
        pawn.builderUnit.OnUnitIdle?.Invoke(pawn.builderUnit);

        currentBuilding = null;
        
    }
}
