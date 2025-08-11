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

        Task targetTask = transformUnit.currentTask; 

        if (targetTask?.targetGameObject == null)
            return;

        foreach (Collider2D collider in colliders)
        {
            if (collider?.gameObject == null)
                continue;

            if (collider.gameObject == targetTask.targetGameObject)
            {
                if (transformUnit.floorAgent != null)
                {
                    Tree tree = collider.gameObject.GetComponent<Tree>();
                    Building building = collider.gameObject.GetComponent<Building>();
                    DecorObject decorObject = collider.gameObject.GetComponent<DecorObject>();

                    if (tree != null && transformUnit.floorAgent.currentFloorIndex == tree.layerIndex)
                    {
                        isCheckColliderHit = true;

                        if (characterMovement != null &&
                            transformUnit.transform.position.x < targetTask.targetGameObject.transform.position.x)
                        {
                            characterMovement.HandleFlipByPosition(targetTask.targetGameObject.transform.position);
                        }
                        continue;
                    }
                    if(building != null && transformUnit.floorAgent.currentFloorIndex == building.LayerIndex)
                    {
                        isCheckColliderHit = true;

                        if (characterMovement != null &&
                            transformUnit.transform.position.x < targetTask.targetGameObject.transform.position.x)
                        {
                            characterMovement.HandleFlipByPosition(targetTask.targetGameObject.transform.position);
                        }
                        continue;
                    }
                    if (decorObject != null && transformUnit.floorAgent.currentFloorIndex == decorObject.layerIndex)
                    {
                        isCheckColliderHit = true;
                        if (characterMovement != null &&
                            transformUnit.transform.position.x < targetTask.targetGameObject.transform.position.x)
                        {
                            characterMovement.HandleFlipByPosition(targetTask.targetGameObject.transform.position);
                        }
                        continue;
                    }
                }
            }
        }

        foreach (var colliderByTransform in collidersByTransform)
        {
            if (colliderByTransform?.gameObject != null &&
                colliderByTransform.gameObject == targetTask.targetGameObject)
            {
                isUnitColliderHit = true;
            }
        }

        if (builderController == null)
            return;

        if (isUnitColliderHit && isCheckColliderHit)
        {
            Tree targetTree = targetTask.targetGameObject.GetComponent<Tree>();
            Building targetBuilding = targetTask.targetGameObject.GetComponent<Building>();
            DecorObject targetDecorObject = targetTask.targetGameObject.GetComponent<DecorObject>();

            if (!(builderController.StateMachine.CurrentState is Builder_ChopState) && targetTree != null && !characterMovement.moving)
            {
                builderController.StateMachine.ChangeState(new Builder_ChopState(builderController, targetTree.gameObject));
            }

            if (!(builderController.StateMachine.CurrentState is Builder_BuildState) && targetBuilding != null && !characterMovement.moving)
            {
                builderController.StateMachine.ChangeState(new Builder_BuildState(builderController, targetBuilding));
            }

            if (!(builderController.StateMachine.CurrentState is Builder_ChopState) && targetDecorObject != null && !characterMovement.moving)
            {
                builderController.StateMachine.ChangeState(new Builder_ChopState(builderController, targetDecorObject.gameObject));
            }

            characterMovement.rb.velocity = Vector2.zero;
            characterMovement.moving = false;
        }
        else if (!isUnitColliderHit && isCheckColliderHit)
        {
            if (transformUnit.currentState != UnitState.Moving)
            {
                MoveToTargetPosition(targetTask);
            }
        }
    }
    private void MoveToTargetPosition(Task task)
    {
        Vector3 targetPosition = task.targetGameObject.transform.position;
        Vector3 currentPosition = transformUnit.transform.position;

        Vector3 direction = (targetPosition - currentPosition).normalized;

        Rigidbody2D rb = transformUnit.GetComponent<Rigidbody2D>();
        float moveSpeed = transformUnit.moveSpeed;
        characterMovement.moving = true;

        if (Vector3.Distance(currentPosition, targetPosition) > 0.1f)
        {
            rb.velocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);
        }
    }
}
