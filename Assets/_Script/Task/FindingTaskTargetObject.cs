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

    private void Awake()
    {
        checkCollider = GetComponent<CircleCollider2D>();
        transformCollider = transform.parent.GetComponent<CircleCollider2D>();
        transformUnit = transform.parent.GetComponent<Unit>();
        characterMovement = transformUnit.characterMovement;
    }

    private void Update()
    {
        CheckOverlapAll();
    }

    private void CheckOverlapAll()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transformCollider.transform.position, checkCollider.radius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject == transformUnit.currentTask.targetGameObject &&
                collider.transform.position.y == transformUnit.transform.position.y)
            {
                characterMovement.StopCoroutine(characterMovement.moveCoroutine);
                characterMovement.moving = false;
                characterMovement.HandleFlipByPosition(transformUnit.currentTask.targetGameObject.transform.position);
                Debug.Log($"Đã tìm thấy đối tượng mục tiêu: {collider.gameObject.name}");
                return;
            }
            if(collider.gameObject == transformUnit.currentTask.targetGameObject)
            {
                characterMovement.HandleFlipByPosition(transformUnit.currentTask.targetGameObject.transform.position);
                Debug.Log($"Đã tìm thấy đối tượng mục tiêu: {collider.gameObject.name}");
            }
        }
    }
}
