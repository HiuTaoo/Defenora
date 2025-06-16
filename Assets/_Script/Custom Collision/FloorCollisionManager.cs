using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorCollisionManager : MonoBehaviour
{
    public static FloorCollisionManager Instance { get; private set; }

    // Dictionary để track collision pairs
    private static Dictionary<(int, int), bool> collisionPairs = new Dictionary<(int, int), bool>();

    // Dictionary để track colliders theo floor
    private Dictionary<int, HashSet<Collider2D>> collidersByFloor = new Dictionary<int, HashSet<Collider2D>>();

    // Performance optimization
    private Dictionary<Collider2D, int> colliderFloorCache = new Dictionary<Collider2D, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Đăng ký collider với floor specific
    public void RegisterCollider(Collider2D collider, int floor)
    {
        if (collider == null) return;

        if (!collidersByFloor.ContainsKey(floor))
            collidersByFloor[floor] = new HashSet<Collider2D>();

        collidersByFloor[floor].Add(collider);
        colliderFloorCache[collider] = floor;

        Debug.Log($"Registered {collider.name} to floor {floor}");
    }

    // Hủy đăng ký collider
    public void UnregisterCollider(Collider2D collider)
    {
        if (collider == null) return;

        if (colliderFloorCache.ContainsKey(collider))
        {
            int floor = colliderFloorCache[collider];
            if (collidersByFloor.ContainsKey(floor))
            {
                collidersByFloor[floor].Remove(collider);
            }
            colliderFloorCache.Remove(collider);
        }
    }

    // Set collision giữa 2 colliders
    public static void SetCollisionBetween(Collider2D col1, Collider2D col2, bool enabled)
    {
        if (col1 == null || col2 == null) return;

        int id1 = col1.GetInstanceID();
        int id2 = col2.GetInstanceID();

        var pair = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
        collisionPairs[pair] = enabled;

        Physics2D.IgnoreCollision(col1, col2, !enabled);
    }

    // Kiểm tra xem 2 colliders có nên va chạm không
    public static bool ShouldCollide(Collider2D col1, Collider2D col2)
    {
        if (col1 == null || col2 == null) return false;

        int id1 = col1.GetInstanceID();
        int id2 = col2.GetInstanceID();

        var pair = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));

        if (collisionPairs.ContainsKey(pair))
            return collisionPairs[pair];

        return true; // Default behavior
    }

    // Update collision cho 1 agent với tất cả objects khác
    public void UpdateCollisionsForAgent(FloorAgent agent)
    {
        if (agent == null) return;

        Collider2D agentCollider = agent.GetComponent<Collider2D>();
        if (agentCollider == null) return;

        int agentFloor = agent.currentFloorIndex;

        // Update collision với environment objects
        UpdateEnvironmentCollisions(agentCollider, agentFloor);

        // Update collision với other agents (sẽ được handle bởi FloorManager)
    }

    // Update collision với environment objects
    private void UpdateEnvironmentCollisions(Collider2D agentCollider, int agentFloor)
    {
        LayerMask allowedLayers = FloorManager.Instance.GetLayerMaskForFloor(agentFloor);

        // Disable collision với tất cả floors khác
        foreach (var kvp in collidersByFloor)
        {
            int floor = kvp.Key;
            if (floor != agentFloor)
            {
                foreach (var collider in kvp.Value)
                {
                    if (collider != null && collider != agentCollider)
                    {
                        SetCollisionBetween(agentCollider, collider, false);
                    }
                }
            }
        }

        // Enable collision với floor hiện tại
        if (collidersByFloor.ContainsKey(agentFloor))
        {
            foreach (var collider in collidersByFloor[agentFloor])
            {
                if (collider != null && collider != agentCollider)
                {
                    SetCollisionBetween(agentCollider, collider, true);
                }
            }
        }
    }

    // Get floor của collider
    public int GetColliderFloor(Collider2D collider)
    {
        if (colliderFloorCache.ContainsKey(collider))
            return colliderFloorCache[collider];

        return -1; // Invalid floor
    }

    // Cleanup null references
    public void CleanupNullReferences()
    {
        List<Collider2D> toRemove = new List<Collider2D>();

        foreach (var kvp in colliderFloorCache)
        {
            if (kvp.Key == null)
                toRemove.Add(kvp.Key);
        }

        foreach (var collider in toRemove)
        {
            UnregisterCollider(collider);
        }
    }
}
