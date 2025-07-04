using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SelectUnitSystem : MonoBehaviour
{
    private UnitManager unitManager;

    public GameObject selectedUnit;
    public GameObject targetBuilding; 
    public bool canMoveSelectedUnit = false;

    private bool isMouseDown = false;
    private bool isDragging = false;
    private Vector2 initialMousePosition;
    private Vector2 initialUnitPosition;
    private float dragThreshold = 0.1f; // Khoảng cách tối thiểu để được tính là drag
    private int targetLayerDrag;

    public System.Action OnSelectUnit;


    private void Update()
    {
        if (GameLoop.Instance.StateMachine.CurrentStateType != GameStateType.Editor)
            return;

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            isDragging = false;
            initialMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            CheckTargetUnit();

            if (selectedUnit != null)
            {
                initialUnitPosition = selectedUnit.transform.position;
            }
        }

        if (Input.GetMouseButton(0) && isMouseDown && selectedUnit != null)
        {
            Vector3 originalPosition = selectedUnit.transform.position;
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float dragDistance = Vector2.Distance(initialMousePosition, currentMousePosition);

            if (selectedUnit.GetComponent<Unit>()) {
                if (!isDragging && dragDistance > dragThreshold)
                {
                    isDragging = true;
                    canMoveSelectedUnit = true;
                }

                if (isDragging && canMoveSelectedUnit)
                {
                    Vector2 offset = currentMousePosition - initialMousePosition;
                    selectedUnit.transform.position = initialUnitPosition + offset;
                    selectedUnit.GetComponent<SpriteRenderer>().sortingOrder = 500;

                }
            }

            
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                FloorAgent floorAgent = selectedUnit.GetComponentInChildren<FloorAgent>();
                CheckTargetLayer(mousePosition);
                if(floorAgent)
                {
                    floorAgent.MoveToFloor(targetLayerDrag);
                    floorAgent.UpdateVisualElements();
                }

                var targetGameObject = FindTargetUnit(LayerMask.GetMask("Building"));
                if(targetGameObject != null)
                    CheckCanRegisterUnit(targetGameObject.GetComponent<Building>());


            }

            isMouseDown = false;
            isDragging = false;
            canMoveSelectedUnit = false;
        }
    }

    private void CheckTargetUnit()
    {
        //LayerMask combinedLayerMask = LayerMask.GetMask("NPC", "Building");
        LayerMask combinedLayerMask = LayerMask.GetMask("NPC");
        GameObject targetUnit = FindTargetUnit(combinedLayerMask);

        if (targetUnit != null)
        {
            if (targetUnit.layer == LayerMask.NameToLayer("NPC"))
            {
                selectedUnit = targetUnit;
                Debug.Log($"Selected NPC: {selectedUnit.name}");
                OnSelectUnit?.Invoke();
            }
            else if (targetUnit.layer == LayerMask.NameToLayer("Building"))
            {
                selectedUnit = targetUnit;
                Debug.Log($"Selected Building: {selectedUnit.name}");
                OnSelectUnit?.Invoke();
            }
        }
        else
        {
            selectedUnit = null;
            Debug.Log("No unit selected");
        }
    }

    public GameObject FindTargetUnit(LayerMask targetLayer)
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition, targetLayer);

        if (hitCollider != null)
        {
            return hitCollider.gameObject;
        }

        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, targetLayer);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private void CheckTargetLayer(Vector3 targetPosition)
    {
        int layerCount = GraphNode.Instance.layerDatas.Length;
        int layer = 0;

        for (int i = layerCount - 1; i >= 0; i--)
        {
            var graph = GraphNode.Instance.layerGraphs[i];
            if (graph.nodes.TryGetValue(Vector3Int.FloorToInt(targetPosition), out Node node))
            {
                if (node != null && node.isWalkable)
                {
                    layer = i;
                    break;
                }
            }
        }
        targetLayerDrag = layer;
    }

    private void CheckCanRegisterUnit(Building building) {
        var unit = selectedUnit.GetComponent<Unit>();

        if (unit == null || building == null || unit.assignedBuilding == building)
            return;

        if (unit.assignedBuilding == null )
        {
            if (building.currentCapacity < building.maxCapacity)
            {
                building.AddUnit(unit);
            }
        }

        else if(unit.assignedBuilding != null) {
            unit.assignedBuilding.RemoveUnit(unit);
            if (building.currentCapacity < building.maxCapacity)
            {
                building.AddUnit(unit);
            }
        }

    }
}