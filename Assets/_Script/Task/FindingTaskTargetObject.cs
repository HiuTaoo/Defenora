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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transformCollider.transform.position, checkCollider.radius);
        Collider2D[] collidersByTransform = Physics2D.OverlapCircleAll(transformCollider.transform.position, transformCollider.radius);
        foreach (Collider2D collider in colliders)
        {
            if(collider.gameObject == transformUnit.currentTask?.targetGameObject &&
                characterMovement.moveCoroutine == null &&
                transformUnit.floorAgent.currentFloorIndex == collider.gameObject.GetComponent<Tree>().layerIndex)
            {
                characterMovement.HandleFlipByPosition(transformUnit.currentTask.targetGameObject.transform.position);
                if(builderController.StateMachine.CurrentState is Builder_ChopState)
                {
                    return;
                }
                builderController.StateMachine.ChangeState(new
                   Builder_ChopState(builderController, transformUnit.currentTask.targetGameObject.GetComponent<Tree>()));
            }
        }
    }
}
