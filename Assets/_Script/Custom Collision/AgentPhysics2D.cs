using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FloorAgent))]
public class AgentPhysics2D : MonoBehaviour
{
    private FloorAgent floorAgent;

    private void Awake()
    {
        floorAgent = GetComponent<FloorAgent>();
    }

    // Raycast với floor filtering
    public RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance)
    {
        return Physics2D.Raycast(origin, direction.normalized, distance, floorAgent.CurrentCollisionMask);
    }

    // OverlapCircle với floor filtering
    public Collider2D[] OverlapCircle(Vector2 center, float radius)
    {
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(center, radius, floorAgent.CurrentCollisionMask);

        // Additional filtering for cross-floor objects
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

    // OverlapBox với floor filtering
    public Collider2D[] OverlapBox(Vector2 center, Vector2 size, float angle = 0f)
    {
        Collider2D[] allColliders = Physics2D.OverlapBoxAll(center, size, angle, floorAgent.CurrentCollisionMask);

        // Additional filtering
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

    // Check if path is blocked
    public bool IsBlocked(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D hit = Raycast(origin, direction, distance);
        return hit.collider != null;
    }

    // Check if path is blocked với CircleCollider
    public bool IsBlocked(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        direction = direction.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, floorAgent.CurrentCollisionMask);

        if (hit.collider != null)
        {
            return floorAgent.CanCollideWith(hit.collider);
        }

        return false;
    }


    // Debug visualization
    public void DrawRay(Vector2 origin, Vector2 direction, float distance, Color color)
    {
        Debug.DrawRay(origin, direction.normalized * distance, color);
    }

    public bool isInStairZone = false;

    public bool IsStair(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        direction = direction.normalized;

        // Kiểm tra va chạm theo hướng di chuyển
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, LayerMask.GetMask("Stair"));
        if (hit.collider != null && floorAgent.CanCollideWith(hit.collider))
        {
            isInStairZone = true;
            return true;
        }

        // Nếu không có va chạm mới, kiểm tra xem đang còn trong vùng cầu thang hay không
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(origin, radius, LayerMask.GetMask("Stair"));
        foreach (var stairCollider in overlaps)
        {
            if (floorAgent.CanCollideWith(stairCollider))
            {
                isInStairZone = true;
                return true;
            }
        }

        // Không còn ở trong vùng stair
        isInStairZone = false;
        return false;
    }


}
