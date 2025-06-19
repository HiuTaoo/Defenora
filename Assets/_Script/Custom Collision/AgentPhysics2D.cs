using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(FloorAgent))]
[RequireComponent(typeof(StairCollision))]
public class AgentPhysics2D : MonoBehaviour
{
    private FloorAgent floorAgent;
    private StairCollision stairDetector;

    private void Awake()
    {
        floorAgent = GetComponent<FloorAgent>();
        stairDetector = GetComponent<StairCollision>();

        if (stairDetector != null)
        {
            stairDetector.OnEnterStair += HandleEnterStair;
            stairDetector.OnExitStair += HandleExitStair;
        }
    }


    public RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance)
    {
        return Physics2D.Raycast(origin, direction.normalized, distance, floorAgent.CurrentCollisionMask);
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

    public bool IsBlocked(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D hit = Raycast(origin, direction, distance);
        return hit.collider != null;
    }

    public bool IsBlock(Vector2 origin, Vector2 direction, float speed, float distance, CircleCollider2D collider)
    {
        if (collider == null)
            return false;

        if (OverlapAll(origin, direction, distance, collider, LayerMask.GetMask("Stair")))
            return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        direction = direction.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, floorAgent.CurrentCollisionMask);

        if (hit.collider != null)
        {
            return floorAgent.CanCollideWith(hit.collider); 
        }

        return false;
    }


    public bool OverlapAll(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider, LayerMask layerMask)
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
            Vector2 start = origin  + new Vector2(-radiusX, offsetY);
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
        Debug.Log("On Stair!");
        transform.GetComponent<SpriteRenderer>().sortingOrder = 305;
        //floorAgent.NextFloor();
    }

    private void HandleExitStair()
    {
        floorAgent.UpdateVisualElements();
        Debug.Log(floorAgent.currentFloorIndex);
    }

}
