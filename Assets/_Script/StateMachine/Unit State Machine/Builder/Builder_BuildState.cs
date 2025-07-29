using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder_BuildState : IUnitState
{
    private BuilderController pawn;
    private Building currentBuilding;

    private bool isCompleted = false;
    private float buildCooldown = 0.5f;

    private Vector2 facingDir;
    private Vector2 origin;

    private Coroutine cooldownCoroutine;

    public Builder_BuildState(BuilderController pawn, Building building)
    {
        this.pawn = pawn;
        currentBuilding = building;
    }

    public void OnEnter()
    {
        pawn.animator.Play("Build");
        pawn.rb.velocity = Vector2.zero;
    }

    public void OnExit() {
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
        facingDir = pawn.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        origin = (Vector2)pawn.transform.position + facingDir * pawn.chopDistance;

        int layer = LayerMask.GetMask($"Building");

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, pawn.chopBoxSize, 0f, layer);

        currentBuilding = null;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Building") && hit.gameObject == pawn.builderUnit.currentTask.targetGameObject)
            {
                currentBuilding = hit.gameObject.GetComponent<Building>();
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
}
