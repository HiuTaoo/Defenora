using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[RequireComponent(typeof(FloorAgent))]
[RequireComponent(typeof(StairCollision))]
[RequireComponent(typeof(CharacterMovement))]
public class AgentPhysics2D : MonoBehaviour
{
    private FloorAgent floorAgent;
    private StairCollision stairDetector;
    private CharacterMovement characterMovement;
    private Vector2 intoStairDirection = Vector2.zero;
    private float moveSpeed;

    private void Awake()
    {
        floorAgent = GetComponent<FloorAgent>();
        stairDetector = GetComponent<StairCollision>();
        characterMovement = GetComponent<CharacterMovement>();

        if (stairDetector != null)
        {
            stairDetector.OnEnterStair += HandleEnterStair;
            stairDetector.OnExitStair += HandleExitStair;
        }
    }

    private void Start()
    {
        if(moveSpeed == 0)
            moveSpeed = GetMoveSpeed();
    }

    public bool PredictRaycast(Vector2 origin, Vector2 direction,float distance, float speed , CircleCollider2D collider, LayerMask layer)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(
            collider.transform.lossyScale.x,
            collider.transform.lossyScale.y
        );
        Vector2 nextPosition = origin + speed * direction *Time.deltaTime;

        RaycastHit2D hit = Physics2D.CircleCast(
        nextPosition,
            radius,
            direction.normalized,
            distance,
            LayerMask.GetMask("Building")
        );

        if (hit.collider != null)
        {
            return true;
        }

        return false;
    }

    public Collider2D[] OverlapCircle(Vector2 center, float radius, LayerMask layer)
    {
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(center, radius, layer);

        List<Collider2D> validColliders = new List<Collider2D>();
        foreach (var collider in allColliders)
        {
            if (floorAgent.CanCollideWith(collider))
            {
                validColliders.Add(collider);
            }
        }

        return validColliders.ToArray();
    }

    public Collider2D[] OverlapBox(Vector2 center, Vector2 size, LayerMask layer, float angle = 0f)
    {
        Collider2D[] allColliders = Physics2D.OverlapBoxAll(center, size, angle, layer);

        List<Collider2D> validColliders = new List<Collider2D>();
        foreach (var collider in allColliders)
        {
            if (floorAgent.CanCollideWith(collider))
            {
                validColliders.Add(collider);
            }
        }

        return validColliders.ToArray();
    }

    public bool IsBlock(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null)
            return false;

        if (OverlapAllSixPoint(origin, direction, distance, collider, LayerMask.GetMask("Stair")))
            return false;

        if (OverlapSideMismatchCheck(origin, direction, distance, collider, LayerMask.GetMask("Stair")))
            return true;

        if(PredictRaycast(origin, direction, distance, moveSpeed, collider, LayerMask.GetMask("Building")))
            return true;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        direction = direction.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, floorAgent.CurrentCollisionMask);

        if (hit.collider != null)
        {
            return floorAgent.CanCollideWith(hit.collider);
        }

        return false;
    }


    public bool IsBuilding(Vector2 origin, Vector2 direction, float distance, float moveSpeed, CircleCollider2D collider)
    {
        if (collider == null)
            return false;

        // Tính bán kính thực tế sau khi scale
        float radius = collider.radius * Mathf.Max(
            collider.transform.lossyScale.x,
            collider.transform.lossyScale.y
        );

        Vector2 nextPosition = origin + direction * moveSpeed * Time.deltaTime;

        // Xác định hướng di chuyển chuẩn hoá
        Vector2 normalizedDirection = direction.normalized;

        // Thực hiện CircleCast từ vị trí hiện tại, mô phỏng di chuyển
        RaycastHit2D hit = Physics2D.CircleCast(
            nextPosition,
            radius,
            normalizedDirection,
            distance,
            LayerMask.GetMask("Building")
        );

        return hit.collider != null;
    }

    public bool OverlapSideMismatchCheck(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider, LayerMask layerMask)
    {
        if (collider == null) return false;

        float radiusX = collider.radius * collider.transform.lossyScale.x;
        float radiusY = collider.radius * collider.transform.lossyScale.y;

        float height = radiusY * 2f;
        float step = height / 3f;

        int hitLeftCount = 0;
        int hitRightCount = 0;

        for (int i = 0; i <= 2; i++)
        {
            float offsetY = -radiusY + i * step;
            Vector2 start = origin + new Vector2(-radiusX, offsetY);
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, layerMask);
            //Debug.DrawRay(start, direction.normalized * distance, Color.red, 0.1f);

            if (hit.collider != null)
            {
                hitLeftCount++;
            }
        }

        for (int i = 0; i <= 2; i++)
        {
            float offsetY = -radiusY + i * step;
            Vector2 start = origin + new Vector2(radiusX, offsetY);
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, layerMask);
            //Debug.DrawRay(start, direction.normalized * distance, Color.blue, 0.1f);

            if (hit.collider != null)
            {
                hitRightCount++;
            }
        }

        return (hitLeftCount == 0 && hitRightCount > 0) || (hitRightCount == 0 && hitLeftCount > 0);
    }


    public bool OverlapAllSixPoint(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider, LayerMask layerMask)
    {
        if (collider == null) return false;

        float radiusX = collider.radius * collider.transform.lossyScale.x;
        float radiusY = collider.radius * collider.transform.lossyScale.y;

        float height = radiusY * 2f;
        float step = height / 3f;

        bool hitLeft = false;
        bool hitRight = false;

        for (int i = 0; i <= 2; i++)
        {
            float offsetY = -radiusY + i * step;
            Vector2 start = origin + new Vector2(-radiusX, offsetY);
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, layerMask);
            //Debug.DrawRay(start, direction.normalized * distance, Color.green, 0.1f);

            if (hit.collider != null)
            {
                hitLeft = true;
                break;
            }
        }

        for (int i = 0; i <= 2; i++)
        {
            float offsetY = -radiusY + i * step;
            Vector2 start = origin + new Vector2(radiusX, offsetY);
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, layerMask);
            //Debug.DrawRay(start, direction.normalized * distance, Color.green, 0.1f);

            if (hit.collider != null)
            {
                hitRight = true;
                break;
            }
        }

        return hitLeft && hitRight;
    }

    private void HandleEnterStair()
    {
        transform.GetComponentInParent<SpriteRenderer>().sortingOrder = 305;
        Vector2 movementInput = GameLoop.Instance.gameContext.InputManager.GetMovementInput();
        if (movementInput.y > 0.1f)
            intoStairDirection = Vector2.up;
        if (movementInput.y < -0.1f)
            intoStairDirection = Vector2.down;
    }

    private void HandleExitStair()
    {
        characterMovement.UpdateLayerIndex();
        floorAgent.UpdateVisualElements();

        Vector2 movementInput = GameLoop.Instance.gameContext.InputManager.GetMovementInput();

        if (movementInput.y > 0.1f && intoStairDirection == Vector2.up)
        {
            floorAgent.MoveToFloor(floorAgent.currentFloorIndex + 1);

        }
        else if (movementInput.y < -0.1f && intoStairDirection == Vector2.down)
        {
            floorAgent.MoveToFloor(floorAgent.currentFloorIndex - 1);
        }
        characterMovement.currentLayer = floorAgent.currentFloorIndex;

    }

    private float GetMoveSpeed() {
        return GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CharacterMovement>().moveSpeed;
    }
}
