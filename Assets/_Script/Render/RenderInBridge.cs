using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(DynamicSortingYX))]
public class RenderInBridge : MonoBehaviour
{
    public int bridgeOffset = 1; 

    private CircleCollider2D circle;
    private SpriteRenderer sr;
    private DynamicSortingYX dynamicSorting;
    private FloorAgent floorAgent;

    private void Awake()
    {
        circle         = GetComponent<CircleCollider2D>();
        sr             = GetComponent<SpriteRenderer>();
        dynamicSorting = GetComponent<DynamicSortingYX>();
        floorAgent = GetComponentInChildren<FloorAgent>();
    }

    private void LateUpdate()
    {
        if (BridgeTilemapManager.Instance.Equals(null))
            return;
        
        Vector3 footPos = circle.bounds.center;

        if (BridgeTilemapManager.Instance.TryGetBridgeSortingOrder(footPos,floorAgent._currentFloorIndex, out int bridgeOrder))
        {
            if (dynamicSorting.enabled)
                dynamicSorting.enabled = false;

            sr.sortingOrder = bridgeOrder + bridgeOffset;
        }
        else
        {
            if (!dynamicSorting.enabled)
                dynamicSorting.enabled = true;
        }
    }
}

