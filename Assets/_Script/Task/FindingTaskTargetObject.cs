using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FindingTaskTargetObject : MonoBehaviour
{
    private CircleCollider2D transformCollider;
    private Unit transformUnit;
    private CharacterMovement characterMovement;
    private CircleCollider2D checkCollider;
    private BuilderController builderController;

    private void Awake()
    {
        checkCollider = GetComponent<CircleCollider2D>();
        transformCollider = transform.parent.GetComponent<CircleCollider2D>();
        transformUnit = transform.parent.GetComponent<Unit>();
        characterMovement = transformUnit.characterMovement;
        builderController = transformUnit.GetComponent<BuilderController>();
    }

    private void Update()
    {
        CheckOverlapAll();
    }

    private void CheckOverlapAll()
    {
        if (transformCollider?.transform == null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transformCollider.transform.position, checkCollider.radius);
        Collider2D[] collidersByTransform = Physics2D.OverlapCircleAll(transformCollider.transform.position, transformCollider.radius);

        bool isUnitColliderHit = false;
        bool isCheckColliderHit = false;

        if (transformUnit.currentTask?.targetGameObject == null)
            return;

        foreach (Collider2D collider in colliders)
        {
            if (collider?.gameObject == null)
                continue;

            if (collider.gameObject == transformUnit.currentTask.targetGameObject)
            {
                if (transformUnit.floorAgent != null)
                {
                    Tree tree = collider.gameObject.GetComponent<Tree>();
                    Building building = collider.gameObject.GetComponent<Building>();
                    if (tree != null && transformUnit.floorAgent.currentFloorIndex == tree.layerIndex)
                    {
                        isCheckColliderHit = true;

                        if (characterMovement != null &&
                            transformUnit.transform.position.x < transformUnit.currentTask.targetGameObject.transform.position.x)
                        {
                            characterMovement.HandleFlipByPosition(transformUnit.currentTask.targetGameObject.transform.position);
                        }
                        continue;
                    }
                    if(building != null && transformUnit.floorAgent.currentFloorIndex == building.LayerIndex)
                    {
                        isCheckColliderHit = true;

                        if (characterMovement != null &&
                            transformUnit.transform.position.x < transformUnit.currentTask.targetGameObject.transform.position.x)
                        {
                            characterMovement.HandleFlipByPosition(transformUnit.currentTask.targetGameObject.transform.position);
                        }
                        continue;
                    }
                }
            }
        }

        foreach (var colliderByTransform in collidersByTransform)
        {
            if (colliderByTransform?.gameObject != null &&
                colliderByTransform.gameObject == transformUnit.currentTask.targetGameObject)
            {
                isUnitColliderHit = true;
            }
        }

        if (builderController == null)
            return;

        if (isUnitColliderHit && isCheckColliderHit)
        {
            Tree targetTree = transformUnit.currentTask.targetGameObject.GetComponent<Tree>();
            Building targetBuilding = transformUnit.currentTask.targetGameObject.GetComponent<Building>();

            characterMovement.rb.velocity = Vector2.zero;
            characterMovement.moving = false;

            if (!(builderController.StateMachine.CurrentState is Builder_ChopState) && targetTree != null)
            {
                builderController.StateMachine.ChangeState(new Builder_ChopState(builderController, targetTree));
            }

            if (!(builderController.StateMachine.CurrentState is Builder_BuildState) && targetBuilding != null)
            {
                builderController.StateMachine.ChangeState(new Builder_BuildState(builderController, targetBuilding));
            }
        }
        else if (!isUnitColliderHit && isCheckColliderHit)
        {
            if (transformUnit.currentState != UnitState.Moving)
            {
                MoveToTargetPosition();
            }
        }
    }
    private void MoveToTargetPosition()
    {
        Vector3 targetPosition = transformUnit.currentTask.targetGameObject.transform.position;
        Vector3 currentPosition = transformUnit.transform.position;

        Vector3 direction = (targetPosition - currentPosition).normalized;

        Rigidbody2D rb = transformUnit.GetComponent<Rigidbody2D>();
        float moveSpeed = transformUnit.moveSpeed;

        if (Vector3.Distance(currentPosition, targetPosition) > 0.1f)
        {
            rb.velocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);
            characterMovement.moving = true;
        }
    }
}
