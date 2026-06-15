using System;
using UnityEngine;

public class StairCollision : MonoBehaviour
{
    private CircleCollider2D circleCollider; 

    public bool IsOnStair { get; private set; } = false;

    public Action OnEnterStair;
    public Action OnExitStair;

    private bool wasOnStair = false;

    private LayerMask stairLayerMask;
    private float calculatedRadius;

    private readonly Collider2D[] hitResults = new Collider2D[1];

    private void Awake()
    {
        circleCollider = GetComponentInParent<CircleCollider2D>();

        stairLayerMask = LayerMask.GetMask("Stair");

        if (circleCollider != null)
            calculatedRadius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
    }

    private void Update()
    {
        bool currentlyOnStair = CheckIfOnStair();

        if (!wasOnStair && currentlyOnStair)
        {
            OnEnterStair?.Invoke();
        }
        else if (wasOnStair && !currentlyOnStair)
        {
            OnExitStair?.Invoke();
        }

        IsOnStair = currentlyOnStair;
        wasOnStair = currentlyOnStair;
    }

    private bool CheckIfOnStair()
    {
        if (circleCollider == null) return false;

        Vector2 origin = transform.position;

        var hitCount = Physics2D.OverlapCircleNonAlloc(origin, calculatedRadius, hitResults, stairLayerMask);

        return hitCount > 0;
    }
}