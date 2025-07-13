using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class SelectUnitSystem : MonoBehaviour
{
    public static SelectUnitSystem Instance;

    private UnitManager unitManager;
    private SpriteRenderer selectUnitSpriteRenderer;
    private FloorAgent floorAgent;

    public GameObject selectedUnit;
    public GameObject targetBuilding;
    public bool canMoveSelectedUnit = false;

    private bool isMouseDown = false;
    private bool isDragging = false;
    private bool isBuilding = false;
    public bool isPlacing = false;
    private float dragThreshold = 0.1f;
    private int targetLayerIndexDrag;

    private Vector2 initialMousePosition;
    private Vector2 initialUnitPosition;
    private int previousLayerIndex;

    public System.Action<GameObject> OnSelectUnit;
    public System.Action<bool> OnDragUnit;
    public System.Action<Vector3> OnLerpToSelectedUnit;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isMouseDown = true;
            isDragging = false;
            initialMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            CheckTargetUnit();

            if (selectedUnit != null && floorAgent != null)
            {
                initialUnitPosition = selectedUnit.transform.position;
                previousLayerIndex = floorAgent.currentFloorIndex;
            }
            else if (selectedUnit == null)
            {
                OnSelectUnit?.Invoke(selectedUnit);
            }
        }

        if (Input.GetMouseButton(0) && isMouseDown && selectedUnit != null)
        {
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float dragDistance = Vector2.Distance(initialMousePosition, currentMousePosition);

            if (!isDragging && dragDistance > dragThreshold)
            {
                isDragging = true;
                canMoveSelectedUnit = !isBuilding;
            }

            if (isDragging && canMoveSelectedUnit)
            {
                Vector2 offset = currentMousePosition - initialMousePosition;
                MoveByCell(initialUnitPosition + offset);
                OnDragUnit?.Invoke(true);
            }

            if (isDragging && !canMoveSelectedUnit)
            {
                OnDragUnit?.Invoke(false);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && selectedUnit != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (CheckCanAddUnit())
                {
                    selectedUnit = null;
                    isDragging = false;
                    canMoveSelectedUnit = false;
                    isMouseDown = false;

                    if (selectedUnit != null)
                        selectedUnit.GetComponentInChildren<FloorAgent>().UpdateVisualElements();

                    OnDragUnit?.Invoke(false);
                    return;
                }

                CheckTargetLayer(mousePosition);
                CheckCanMovePlayerTo(mousePosition);
                //OnSelectUnit?.Invoke(false, false);
            }

            if (selectedUnit != null && !isDragging && !isPlacing) {
                OnSelectUnit?.Invoke(selectedUnit);
                OnLerpToSelectedUnit?.Invoke(selectedUnit.transform.position);
            }

            isMouseDown = false;
            isDragging = false;
            canMoveSelectedUnit = false;
            selectedUnit = null;
        }
    }

    private void MoveByCell(Vector3 worldPos)
    {
        float cellSize = 1f; 

        int cellX = Mathf.FloorToInt(worldPos.x / cellSize);
        int cellY = Mathf.FloorToInt(worldPos.y / cellSize);

        Vector3 snappedPos = new Vector3(
            (cellX + 0.5f) * cellSize,
            (cellY + 0.5f) * cellSize,
            0f
        );

        selectedUnit.transform.position = snappedPos;

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
    public void CancelPlaceBuilding()
    {
        isPlacing = false;
        BuildingGhostPreviewSystem.Instance.currentGhost = null;
        MenuItem menuItem = FindObjectOfType<MenuItem>();
        menuItem.DeSelectAllTileItem();
        MenuEditorController.Instance.cancelEditBuildingMode.SetActive(false);
    }
    #region Check
    private void CheckTargetUnit()
    {
        LayerMask combinedLayerMask = LayerMask.GetMask("NPC", "Building");
        GameObject targetUnit = FindTargetUnit(combinedLayerMask);

        if (targetUnit != null)
        {
            if (targetUnit.layer == LayerMask.NameToLayer("NPC"))
            {
                selectedUnit = targetUnit;
                isBuilding = false;
                selectUnitSpriteRenderer = selectedUnit.GetComponent<SpriteRenderer>();
                floorAgent = selectedUnit.GetComponentInChildren<FloorAgent>();
            }
            else if (targetUnit.layer == LayerMask.NameToLayer("Building"))
            {
                selectedUnit = targetUnit;
                selectUnitSpriteRenderer = selectedUnit.GetComponent<SpriteRenderer>();
                floorAgent = selectedUnit.GetComponentInChildren<FloorAgent>();
                isBuilding = true;
            }
        }
        else
        {
            isBuilding = false;
            selectedUnit = null;
        }
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
        targetLayerIndexDrag = layer;
    }

    private bool CheckCanAddUnit()
    {
        var targetGameObject = FindTargetUnit(LayerMask.GetMask("Building"));

        if (targetGameObject != null)
            if (CheckCanRegisterUnit(targetGameObject.GetComponent<Building>()))
                return true;
        return false;
    }

    private bool CheckCanRegisterUnit(Building building)
    {
        var unit = selectedUnit.GetComponent<Unit>();

        if (unit.assignedBuilding == null)
        {
            if (building.currentCapacity < building.maxCapacity)
            {
                building.AddUnit(unit);
                return true;
            }
        }

        else if (unit.assignedBuilding != null && unit.assignedBuilding != building)
        {
            unit.assignedBuilding.RemoveUnit(unit);
            if (building.currentCapacity < building.maxCapacity)
            {
                building.AddUnit(unit);
                return true;
            }
        }
        return false;
    }

    private void CheckCanMovePlayerTo(Vector3 targetPosition)
    {
        float cellSize = 1f;
        if (selectedUnit.GetComponent<Building>() != null)
            return;

        int cellX = Mathf.FloorToInt(targetPosition.x / cellSize);
        int cellY = Mathf.FloorToInt(targetPosition.y / cellSize);

        Vector3 snappedPos = new Vector3(
            (cellX + 0.5f) * cellSize,
            (cellY + 0.5f) * cellSize,
            0f
        );

        Node node = GraphNode.Instance.GetNode(Vector3Int.FloorToInt(snappedPos), targetLayerIndexDrag);

        if (node != null && node.isWalkable)
        {
            selectedUnit.transform.position = snappedPos;
            floorAgent.MoveToFloor(targetLayerIndexDrag);
            floorAgent.UpdateVisualElements();
        }
        else
        {
            selectedUnit.transform.position = initialUnitPosition;
            floorAgent.MoveToFloor(previousLayerIndex);
            floorAgent.UpdateVisualElements();
        }

    }
    #endregion
}