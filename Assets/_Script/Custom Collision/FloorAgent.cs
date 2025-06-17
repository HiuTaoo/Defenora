using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloorAgent : MonoBehaviour
{
    public int _currentFloorIndex
    {
        get;
        private set;
    }

    // Properties
    public int currentFloorIndex
    {
        get => _currentFloorIndex;
        private set
        {
            if (value > 2)
            {
                return;
            }

            _currentFloorIndex = value;
        }
    }


    public LayerMask CurrentCollisionMask
    {
        get => FloorManager.Instance.GetLayerMaskForFloor(currentFloorIndex);
    }

    // Events
    public System.Action<int, int> OnFloorChanged;

    // Components
    private Collider2D _collider;
    public Collider2D Collider => _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _currentFloorIndex = 0;
    }

    private void Start()
    {
        // Đăng ký với FloorManager
        FloorManager.Instance.RegisterAgent(this);

        // Update initial collisions
        FloorCollisionManager.Instance.UpdateCollisionsForAgent(this);
    }

    private void OnDestroy()
    {
        // Hủy đăng ký
        FloorManager.Instance?.UnregisterAgent(this);
    }

    // Chuyển floor
    public void MoveToFloor(int floorIndex)
    {
        if (floorIndex == currentFloorIndex) return;

        int oldFloor = currentFloorIndex;

        // Sử dụng FloorManager để handle việc chuyển floor
        FloorManager.Instance.MoveAgentToFloor(this, floorIndex);

        // Trigger local event
        OnFloorChanged?.Invoke(oldFloor, floorIndex);

        // Update visual elements (sorting layer, etc.)
        UpdateVisualElements();
    }

    public void NextFloor() { 
        if(currentFloorIndex == FloorManager.Instance.floors.Count - 1)
            return;

        _currentFloorIndex++;
        MoveToFloor(currentFloorIndex);
    }

    public void PreviousFloor() { 
        if( currentFloorIndex == 0)
            return;

        _currentFloorIndex--;
        MoveToFloor(currentFloorIndex);
    }

    public void UpdateVisualElements()
    {
        var floorDef = FloorManager.Instance.GetFloorDefinition(currentFloorIndex);
        if (floorDef != null)
        {
            // Update sorting order
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = floorDef.sortingOrder;
            }

            // Update other visual elements as needed
        }
    }

    // Kiểm tra xem có thể va chạm với object không
    public bool CanCollideWith(Collider2D other)
    {
        if (other == null) return false;

        // Check if it's another agent
        FloorAgent otherAgent = other.GetComponent<FloorAgent>();
        if (otherAgent != null)
        {
            return otherAgent.currentFloorIndex == this.currentFloorIndex;
        }

        // Check environment objects
        int floor = FloorCollisionManager.Instance.GetColliderFloor(other);
        if (floor >= 0)
        {
            return floor == this.currentFloorIndex;
        }

        // Check layer mask
        int objectLayer = other.gameObject.layer;
        LayerMask currentMask = CurrentCollisionMask;
        return (currentMask & (1 << objectLayer)) != 0;
    }
    public void SetCurrentFloorIndex(int index)
    {
        _currentFloorIndex = index;
    }

    public void PrintDebugInfo()
    {
        Debug.Log($"Agent: {name}, Floor: {currentFloorIndex}, LayerMask: {CurrentCollisionMask}");
    }
}

