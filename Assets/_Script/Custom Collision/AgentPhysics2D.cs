using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(FloorAgent))]
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
    public bool IsBlocked(Vector2 origin, Vector2 direction, float speed, float distance, CircleCollider2D collider)
    {
        if (collider == null)
            return false;

        if (IsStair(origin, direction, distance, collider))
            return false;

        if (IsInBridge(origin, direction, speed, distance, collider))
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



    /*public bool IsStair(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        direction = direction.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, LayerMask.GetMask("Stair"));

        if (hit.collider != null)
        {
            return true;
        }

        return false;
    }*/


    public bool IsStair(Vector2 origin, Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radiusX = collider.radius * collider.transform.lossyScale.x;
        float radiusY = collider.radius * collider.transform.lossyScale.y;

        float height = radiusY * 2f;
        float step = height / 3f;

        int stairMask = LayerMask.GetMask("Stair");

        bool hitLeft = false;
        bool hitRight = false;

        for (int i = 0; i <= 2; i++)
        {
            float offsetY = -radiusY + i * step;
            Vector2 start = origin + new Vector2(-radiusX, offsetY);
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, stairMask);
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
            RaycastHit2D hit = Physics2D.Raycast(start, direction.normalized, distance, stairMask);
            //Debug.DrawRay(start, direction.normalized * distance, Color.green, 0.1f);

            if (hit.collider != null)
            {
                hitRight = true;
                break;
            }
        }

        return hitLeft && hitRight;
    }

    /*public bool IsInBridge(Vector2 origin,Vector2 direction, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        int bridgeMask = LayerMask.GetMask("Bridge");

        Collider2D hitBridge = Physics2D.OverlapCircle(origin, radius, bridgeMask);
        if (hitBridge == null) return false;

        Vector2 top = origin + Vector2.up * radius;
        Vector2 bottom = origin + Vector2.down * radius;
        Vector2 left = origin + Vector2.left * radius;
        Vector2 right = origin + Vector2.right * radius;

        float checkRadius = 0.05f; 

        bool hitTop = Physics2D.OverlapCircle(top, checkRadius, bridgeMask);
        bool hitBottom = Physics2D.OverlapCircle(bottom, checkRadius, bridgeMask);
        bool hitLeft = Physics2D.OverlapCircle(left, checkRadius, bridgeMask);
        bool hitRight = Physics2D.OverlapCircle(right, checkRadius, bridgeMask);

        direction = direction.normalized;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, floorAgent.CurrentCollisionMask);

        if (!hitTop || !hitBottom || !hitLeft || !hitRight)
            return false;

        if (hit.collider != null)
        {
            if (hitTop && hitBottom && hitLeft && hitRight)
                return true;
        }
        
        
        return false;
    }*/

    public bool IsInBridge(Vector2 origin, Vector2 direction,float speed, float distance, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        int bridgeMask = LayerMask.GetMask("Bridge");

        Collider2D hitBridge = Physics2D.OverlapCircle(origin, radius, bridgeMask);
        if (hitBridge == null) return false;

        Vector2 predictedCenter = origin + direction.normalized * speed * Time.fixedDeltaTime;

        Vector2 top = predictedCenter + Vector2.up * radius;
        Vector2 bottom = predictedCenter + Vector2.down * radius;
        Vector2 left = predictedCenter + Vector2.left * radius;
        Vector2 right = predictedCenter + Vector2.right * radius;

        float raycastDistance = 0.1f;

        bool hitTop = Physics2D.Raycast(top, (origin - top).normalized, raycastDistance, bridgeMask).collider != null;
        bool hitBottom = Physics2D.Raycast(bottom, (origin - bottom).normalized, raycastDistance, bridgeMask).collider != null;
        bool hitLeft = Physics2D.Raycast(left, (origin - left).normalized, raycastDistance, bridgeMask).collider != null;
        bool hitRight = Physics2D.Raycast(right, (origin - right).normalized, raycastDistance, bridgeMask).collider != null;

        /*float checkRadius = 0.05f;

        Vector2 top = predictedCenter + Vector2.up * radius;
        Vector2 bottom = predictedCenter + Vector2.down * radius;
        Vector2 left = predictedCenter + Vector2.left * radius;
        Vector2 right = predictedCenter + Vector2.right * radius;

        bool hitTop = Physics2D.OverlapCircle(top, checkRadius, bridgeMask);
        bool hitBottom = Physics2D.OverlapCircle(bottom, checkRadius, bridgeMask);
        bool hitLeft = Physics2D.OverlapCircle(left, checkRadius, bridgeMask);
        bool hitRight = Physics2D.OverlapCircle(right, checkRadius, bridgeMask);*/

        /*direction = direction.normalized;
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, floorAgent.CurrentCollisionMask);

        if (hit.collider != null)
        {
            return hitTop && hitBottom && hitLeft && hitRight;
        }

        return false;*/
        return hitTop && hitBottom && hitLeft && hitRight;
    }

    public bool CanMoveInsideBridge(Vector2 origin, Vector2 direction, float speed, CircleCollider2D collider)
    {
        if (collider == null) return false;

        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        int bridgeMask = LayerMask.GetMask("Bridge");
        float checkRadius = 0.05f;

        Vector2 predictedCenter = origin + direction.normalized * speed * Time.fixedDeltaTime;

        Vector2 top = predictedCenter + Vector2.up * radius;
        Vector2 bottom = predictedCenter + Vector2.down * radius;
        Vector2 left = predictedCenter + Vector2.left * radius;
        Vector2 right = predictedCenter + Vector2.right * radius;

        bool hitTop = Physics2D.OverlapCircle(top, checkRadius, bridgeMask);
        bool hitBottom = Physics2D.OverlapCircle(bottom, checkRadius, bridgeMask);
        bool hitLeft = Physics2D.OverlapCircle(left, checkRadius, bridgeMask);
        bool hitRight = Physics2D.OverlapCircle(right, checkRadius, bridgeMask);

        return hitTop && hitBottom && hitLeft && hitRight;
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
