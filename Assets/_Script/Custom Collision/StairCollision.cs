using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StairCollision : MonoBehaviour
{
    private CircleCollider2D circleCollider; 

    public bool IsOnStair { get; private set; } = false;

    public System.Action OnEnterStair;
    public System.Action OnExitStair;

    private bool wasOnStair = false;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        bool currentlyOnStair = CheckIfOnStair();

        if (!wasOnStair && currentlyOnStair)
        {
            // Enter stair
            OnEnterStair?.Invoke();
        }
        else if (wasOnStair && !currentlyOnStair)
        {
            // Exit stair
            OnExitStair?.Invoke();
        }

        IsOnStair = currentlyOnStair;
        wasOnStair = currentlyOnStair;
    }

    private bool CheckIfOnStair()
    {
        if (circleCollider == null) return false;

        Vector2 origin = transform.position;
        float radius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        Collider2D hit = Physics2D.OverlapCircle(origin, radius , LayerMask.GetMask("Stair"));
        return hit != null;
    }

    /*private void OnDrawGizmos()
    {
        if (circleCollider == null) return;

        float radius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        // Màu tùy vào trạng thái
        Gizmos.color = IsOnStair ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawSphere(transform.position, 0.5f); // chấm tâm
    }*/
}
