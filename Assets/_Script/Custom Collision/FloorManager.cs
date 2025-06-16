using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [SerializeField] private List<FloorDefinition> floors = new List<FloorDefinition>();

    // Dictionary để track agents theo floor
    private Dictionary<int, HashSet<FloorAgent>> agentsByFloor = new Dictionary<int, HashSet<FloorAgent>>();

    // Events
    public System.Action<FloorAgent, int, int> OnAgentFloorChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFloors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFloors()
    {
        foreach (var floor in floors)
        {
            agentsByFloor[floor.floorIndex] = new HashSet<FloorAgent>();
        }
    }

    // Đăng ký agent
    public void RegisterAgent(FloorAgent agent)
    {
        if (agent == null) return;

        int floor = agent.currentFloorIndex;

        if (!agentsByFloor.ContainsKey(floor))
            agentsByFloor[floor] = new HashSet<FloorAgent>();

        agentsByFloor[floor].Add(agent);

        // Update collisions với tất cả agents khác
        UpdateAllAgentCollisions();

        Debug.Log($"Registered agent {agent.name} to floor {floor}");
    }

    // Hủy đăng ký agent
    public void UnregisterAgent(FloorAgent agent)
    {
        if (agent == null) return;

        foreach (var kvp in agentsByFloor)
        {
            kvp.Value.Remove(agent);
        }

        // Cleanup collisions
        Collider2D agentCollider = agent.GetComponent<Collider2D>();
        if (agentCollider != null)
        {
            FloorCollisionManager.Instance.UnregisterCollider(agentCollider);
        }
    }

    // Di chuyển agent sang floor mới
    public void MoveAgentToFloor(FloorAgent agent, int newFloor)
    {
        if (agent == null || newFloor < 0 || newFloor >= floors.Count) return;

        int oldFloor = agent.currentFloorIndex;

        // Remove from old floor
        if (agentsByFloor.ContainsKey(oldFloor))
        {
            agentsByFloor[oldFloor].Remove(agent);
        }

        // Add to new floor
        agent.SetCurrentFloorIndex(newFloor);
        if (!agentsByFloor.ContainsKey(newFloor))
            agentsByFloor[newFloor] = new HashSet<FloorAgent>();

        agentsByFloor[newFloor].Add(agent);

        // Trigger event
        OnAgentFloorChanged?.Invoke(agent, oldFloor, newFloor);

        // Update collisions
        UpdateCollisionsForMovedAgent(agent, oldFloor, newFloor);

        Debug.Log($"Moved agent {agent.name} from floor {oldFloor} to floor {newFloor}");
    }

    // Update collisions khi agent chuyển floor
    private void UpdateCollisionsForMovedAgent(FloorAgent agent, int oldFloor, int newFloor)
    {
        Collider2D agentCollider = agent.GetComponent<Collider2D>();
        if (agentCollider == null) return;

        // Disable collision với agents ở floor cũ
        if (agentsByFloor.ContainsKey(oldFloor))
        {
            foreach (var otherAgent in agentsByFloor[oldFloor])
            {
                if (otherAgent != null && otherAgent != agent)
                {
                    Collider2D otherCollider = otherAgent.GetComponent<Collider2D>();
                    if (otherCollider != null)
                    {
                        FloorCollisionManager.SetCollisionBetween(agentCollider, otherCollider, false);
                    }
                }
            }
        }

        // Enable collision với agents ở floor mới
        if (agentsByFloor.ContainsKey(newFloor))
        {
            foreach (var otherAgent in agentsByFloor[newFloor])
            {
                if (otherAgent != null && otherAgent != agent)
                {
                    Collider2D otherCollider = otherAgent.GetComponent<Collider2D>();
                    if (otherCollider != null)
                    {
                        FloorCollisionManager.SetCollisionBetween(agentCollider, otherCollider, true);
                    }
                }
            }
        }

        // Update environment collisions
        FloorCollisionManager.Instance.UpdateCollisionsForAgent(agent);
    }

    // Update collision cho tất cả agents
    private void UpdateAllAgentCollisions()
    {
        // Disable collision giữa agents ở floors khác nhau
        foreach (var floor1 in agentsByFloor)
        {
            foreach (var floor2 in agentsByFloor)
            {
                if (floor1.Key != floor2.Key)
                {
                    // Disable collision giữa 2 floors khác nhau
                    foreach (var agent1 in floor1.Value)
                    {
                        foreach (var agent2 in floor2.Value)
                        {
                            if (agent1 != null && agent2 != null)
                            {
                                Collider2D col1 = agent1.GetComponent<Collider2D>();
                                Collider2D col2 = agent2.GetComponent<Collider2D>();

                                if (col1 != null && col2 != null)
                                {
                                    FloorCollisionManager.SetCollisionBetween(col1, col2, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Enable collision trong cùng floor
                    foreach (var agent1 in floor1.Value)
                    {
                        foreach (var agent2 in floor1.Value)
                        {
                            if (agent1 != null && agent2 != null && agent1 != agent2)
                            {
                                Collider2D col1 = agent1.GetComponent<Collider2D>();
                                Collider2D col2 = agent2.GetComponent<Collider2D>();

                                if (col1 != null && col2 != null)
                                {
                                    FloorCollisionManager.SetCollisionBetween(col1, col2, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Get LayerMask cho floor
    public LayerMask GetLayerMaskForFloor(int floorIndex)
    {
        if (floorIndex >= 0 && floorIndex < floors.Count)
            return floors[floorIndex].collisionMask;

        return 0;
    }

    // Get floor definition
    public FloorDefinition GetFloorDefinition(int floorIndex)
    {
        if (floorIndex >= 0 && floorIndex < floors.Count)
            return floors[floorIndex];

        return null;
    }

    // Utility methods
    public string GetFloorName(int index)
    {
        var floor = GetFloorDefinition(index);
        return floor != null ? floor.floorName : "Unknown";
    }

    public int GetFloorCount() => floors.Count;

    public List<FloorAgent> GetAgentsOnFloor(int floorIndex)
    {
        if (agentsByFloor.ContainsKey(floorIndex))
            return new List<FloorAgent>(agentsByFloor[floorIndex]);

        return new List<FloorAgent>();
    }

    // Cleanup null references
    private void Update()
    {
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            CleanupNullReferences();
        }
    }

    private void CleanupNullReferences()
    {
        foreach (var kvp in agentsByFloor)
        {
            kvp.Value.RemoveWhere(agent => agent == null);
        }

        FloorCollisionManager.Instance?.CleanupNullReferences();
    }
}

