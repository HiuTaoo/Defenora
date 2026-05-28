using System.Collections;
using System.Collections.Generic;
using _Script.Enum;
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
        if (GameManager.Instance.StateMachine.CurrentStateType != GameStateType.Editor)
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
            else if (selectedUnit == null &&  targetBuilding == null)
            {
                OnSelectUnit?.Invoke(selectedUnit);
            }
        }

        if (Input.GetMouseButton(0) && isMouseDown && selectedUnit != null)
        {
            if (selectedUnit.gameObject.CompareTag("Enemy"))
                return;
            
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
                selectUnitSpriteRenderer.sortingOrder = 99999;

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

                    OnDragUnit?.Invoke(false);
                    return;
                }
                else
                    selectedUnit.transform.position = initialUnitPosition;

                CheckTargetLayer(mousePosition);
                CheckCanMovePlayerTo(mousePosition);
                //OnSelectUnit?.Invoke(false, false);
            }

            /*if (selectedUnit != null && !isDragging && !isPlacing) {
                OnSelectUnit?.Invoke(selectedUnit);
                OnLerpToSelectedUnit?.Invoke(selectedUnit.transform.position);
            }*/
            
            if (!isDragging && !isPlacing && isMouseDown) 
            {
                if (selectedUnit != null) 
                {
                    OnSelectUnit?.Invoke(selectedUnit);
                    OnLerpToSelectedUnit?.Invoke(selectedUnit.transform.position);
                }
                else if (targetBuilding != null) 
                {
                    OnSelectUnit?.Invoke(targetBuilding);
                    OnLerpToSelectedUnit?.Invoke(targetBuilding.transform.position);
                }
            }

            isMouseDown = false;
            isDragging = false;
            canMoveSelectedUnit = false;
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
        
        targetBuilding = null;
        selectedUnit = null;
    }
    #region Check
    private void CheckTargetUnit()
    {
        LayerMask combinedLayerMask = LayerMask.GetMask("NPC", "Building");
        GameObject clickedObj = FindTargetUnit(combinedLayerMask);

        if (clickedObj != null)
        {
            if (clickedObj.layer == LayerMask.NameToLayer("NPC"))
            {
                selectedUnit = clickedObj;
                targetBuilding = null; 
                isBuilding = false;
                
                selectUnitSpriteRenderer = selectedUnit.GetComponent<SpriteRenderer>();
                floorAgent = selectedUnit.GetComponentInChildren<FloorAgent>();
            }
            else if (clickedObj.layer == LayerMask.NameToLayer("Building"))
            {
                targetBuilding = clickedObj;
                selectedUnit = null; 
                isBuilding = true;
                
                selectUnitSpriteRenderer = targetBuilding.GetComponent<SpriteRenderer>();
                floorAgent = targetBuilding.GetComponentInChildren<FloorAgent>();
            }
        }
        else
        {
            isBuilding = false;
            selectedUnit = null;
            targetBuilding = null;
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
        if(unit == null)
        {
            Debug.LogError("Unit is null in CheckCanRegisterUnit");
            return false;
        }
        if (building == null)
        {
            Debug.LogError("Building is null in CheckCanRegisterUnit");
            return false;
        }

        if (unit.assignedBuilding == null)
        {
            if (building.CanAddUnit(unit))
            {
                building.AddUnit(unit);
                return true;
            }
        }

        else if (unit.assignedBuilding != null && unit.assignedBuilding != building 
            && building.CanAddUnit(unit))
        {
            if (building.currentCapacity < building.maxCapacity)
            {
                unit.assignedBuilding.RemoveUnit(unit);
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
        }
        else
        {
            selectedUnit.transform.position = initialUnitPosition;
            floorAgent.MoveToFloor(previousLayerIndex);
        }
    }

    public void DeleteBuilding()
    {
        if (targetBuilding == null) return;
        
        var building = targetBuilding.GetComponent<Building>();
        if (building == null || building.buildingState != BuildingState.Placing)
            return;

        ObjectFootprint footprint = targetBuilding.GetComponent<ObjectFootprint>();
        if (footprint != null && EditBuildingManager.Instance != null)
        {
            Vector2Int anchorCell = building.WorldToCell(targetBuilding.transform.position, 1f);
            
            var cells = footprint.GetAbsoluteGridPositions(anchorCell);

            foreach (var cell in cells)
            {
                EditBuildingManager.Instance.occupiedCells.Remove(cell);
            }
        }
        
        if (EditBuildingManager.Instance != null && EditBuildingManager.Instance.listPlacedBuilding.Contains(building))
        {
            EditBuildingManager.Instance.listPlacedBuilding.Remove(building);
        }
        
        PoolManager.Instance.Despawn(targetBuilding);
        targetBuilding = null;
    
        OnSelectUnit?.Invoke(null);
    }
    #endregion
}