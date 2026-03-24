using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorAgent : MonoBehaviour
{
    public int _currentFloorIndex
    {
        get;
        private set;
    }

    public int currentFloorIndex
    {
        get => _currentFloorIndex;
        private set
        {
            if (value > 2)
            {
                return;
            }
            Debug.Log($"Change floor index to {value}");
            _currentFloorIndex = value;
        }
    }


    public LayerMask CurrentCollisionMask
    {
        get => FloorManager.Instance.GetLayerMaskForFloor(currentFloorIndex);
    }

    public System.Action<int, int> OnFloorChanged;

    private Collider2D _collider;
    public Collider2D Collider => _collider;

    private void Awake()
    {
        _collider = GetComponentInParent<Collider2D>();
        _currentFloorIndex = 0;
    }

    private void Start()
    {
        FloorManager.Instance.RegisterAgent(this);

        FloorCollisionManager.Instance.UpdateCollisionsForAgent(this);
    }

    private void OnDestroy()
    {
        FloorManager.Instance?.UnregisterAgent(this);
    }

    public void MoveToFloor(int floorIndex)
    {
        if (floorIndex == currentFloorIndex) return;

        int oldFloor = currentFloorIndex;

        FloorManager.Instance.MoveAgentToFloor(this, floorIndex);

        OnFloorChanged?.Invoke(oldFloor, floorIndex);

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
    public bool CanCollideWith(Collider2D other)
    {
        if (other == null) return false;

        FloorAgent otherAgent = other.GetComponent<FloorAgent>();
        if (otherAgent != null)
        {
            return otherAgent.currentFloorIndex == this.currentFloorIndex;
        }

        int floor = FloorCollisionManager.Instance.GetColliderFloor(other);
        if (floor >= 0)
        {
            return floor == this.currentFloorIndex;
        }

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

