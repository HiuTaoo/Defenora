using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [SerializeField] public List<FloorDefinition> floors = new List<FloorDefinition>();

    private Dictionary<int, HashSet<FloorAgent>> agentsByFloor = new Dictionary<int, HashSet<FloorAgent>>();

    public System.Action<FloorAgent, int, int> OnAgentFloorChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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

    public void RegisterAgent(FloorAgent agent)
    {
        if (agent == null) return;

        int floor = agent.currentFloorIndex;

        if (!agentsByFloor.ContainsKey(floor))
            agentsByFloor[floor] = new HashSet<FloorAgent>();

        agentsByFloor[floor].Add(agent);

        UpdateAllAgentCollisions();

        //Debug.Log($"Registered agent {agent.name} to floor {floor}");
    }

    public void UnregisterAgent(FloorAgent agent)
    {
        if (agent == null) return;

        foreach (var kvp in agentsByFloor)
        {
            kvp.Value.Remove(agent);
        }

        Collider2D agentCollider = agent.GetComponent<Collider2D>();
        if (agentCollider != null)
        {
            FloorCollisionManager.Instance.UnregisterCollider(agentCollider);
        }
    }

    public void MoveAgentToFloor(FloorAgent agent, int newFloor)
    {
        if (agent == null || newFloor < 0 || newFloor >= floors.Count) return;

        int oldFloor = agent.currentFloorIndex;

        if (agentsByFloor.ContainsKey(oldFloor))
        {
            agentsByFloor[oldFloor].Remove(agent);
        }

        agent.SetCurrentFloorIndex(newFloor);
        if (!agentsByFloor.ContainsKey(newFloor))
            agentsByFloor[newFloor] = new HashSet<FloorAgent>();

        agentsByFloor[newFloor].Add(agent);

        OnAgentFloorChanged?.Invoke(agent, oldFloor, newFloor);

        UpdateCollisionsForMovedAgent(agent, oldFloor, newFloor);

        //Debug.Log($"Moved agent {agent.name} from floor {oldFloor} to floor {newFloor}");
    }

    private void UpdateCollisionsForMovedAgent(FloorAgent agent, int oldFloor, int newFloor)
    {
        Collider2D agentCollider = agent.GetComponent<Collider2D>();
        if (agentCollider == null) return;

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

        FloorCollisionManager.Instance.UpdateCollisionsForAgent(agent);
    }

    private void UpdateAllAgentCollisions()
    {
        foreach (var floor1 in agentsByFloor)
        {
            foreach (var floor2 in agentsByFloor)
            {
                if (floor1.Key != floor2.Key)
                {
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

    public LayerMask GetLayerMaskForFloor(int floorIndex)
    {
        if (floorIndex >= 0 && floorIndex < floors.Count)
            return floors[floorIndex].collisionMask;

        return 0;
    }

    public FloorDefinition GetFloorDefinition(int floorIndex)
    {
        if (floorIndex >= 0 && floorIndex < floors.Count)
            return floors[floorIndex];

        return null;
    }

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

