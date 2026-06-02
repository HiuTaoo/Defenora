using System;
using System.Collections.Generic;
using _Script.Enum;
using UnityEngine;

public class EditBuildingManager : MonoBehaviour
{
    public static EditBuildingManager Instance;
    public HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    public List<Building> listPlacedBuilding = new List<Building>();

    private GraphNode graphNode;
    private SaveLoadSystem saveLoadSystem;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        graphNode = FindObjectOfType<GraphNode>();
        saveLoadSystem = FindObjectOfType<SaveLoadSystem>();

        if (graphNode == null)
            Debug.LogError("GraphNode không tồn tại trên scene!");
    }

    public bool IsCellOccupied(Vector2Int cellPos)
    {
        return occupiedCells.Contains(cellPos);
    }

    public void PlaceBuilding(Vector2Int anchorCell, GameObject buildingPrefab)
    {
        ObjectFootprint footprint = buildingPrefab.GetComponent<ObjectFootprint>();
        var cells = footprint.GetAbsoluteGridPositions(anchorCell);
        foreach (var cell in cells)
        {
            occupiedCells.Add(cell);
        }

        GameObject currentbuilding = PoolManager.Instance.Spawn(footprint.gameObject, CellToWorld(anchorCell), Quaternion.identity);
        currentbuilding.transform.SetParent(this.gameObject.transform);

        Building building = currentbuilding.GetComponent<Building>();
        building.LayerIndex = LayerManager.Instance.layerIndex;
        building.buildingState = BuildingState.Placing;

        var color = new Color(0, 1, 0, 0.75f);
        var renderer = currentbuilding.GetComponent<SpriteRenderer>();
        renderer.color = color;
        
        currentbuilding.transform.name = $"{building.buildingType}: {Guid.NewGuid()}";
        Debug.Log($"Building: {building.name} is in Placing State");

        listPlacedBuilding.Add(building);
    }

    public bool CanPlaceFootprint(Vector2Int anchorCell, ObjectFootprint footprint, int layerIndex)
    {
        var cells = footprint.GetAbsoluteGridPositions(anchorCell);

        int minX = int.MaxValue;
        int maxX = int.MinValue;

        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
        }

        foreach (var cell in cells)
        {
            bool isLeftEdge = (cell.x == minX);
            bool isRightEdge = (cell.x == maxX);

            if (!isLeftEdge && !isRightEdge)
            {
                if (occupiedCells.Contains(cell))
                    return false;
            }

            Node node = graphNode.GetNode(new Vector3Int(cell.x, cell.y, 0), layerIndex);
            if (node == null || !node.isWalkable || node.isStair)
                return false;

            if (cell == anchorCell && occupiedCells.Contains(cell))
                return false;
        }
        return true;
    }

    public Vector3 CellToWorld(Vector2Int cellPos)
    {
        float cellSize = 1f;
        return new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, 0f);
    }

    public void RollBackBuildingVisual()
    {
        foreach (var building in listPlacedBuilding)
        {
            var spriteRenderer = building.transform.GetComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;
        }
    }

    private void UpdateGraphNode()
    {
        foreach (var building in listPlacedBuilding)
        {
            var foothPrint = building.transform.GetComponent<ObjectFootprint>();
            var cells = foothPrint.GetAbsoluteGridPositions(building.WorldToCell(building.transform.position, 1f));
            foreach (var cell in cells)
            {
                Node node = GraphNode.Instance.GetNode(new Vector3Int(cell.x, cell.y, 0), building.LayerIndex);
                if (node.isWalkable)
                    node.isWalkable = false;
            }
        }
    }

    private void ChangeBuildingState()
    {
        if (listPlacedBuilding != null)
        {
            foreach (var build in listPlacedBuilding)
            {
                build.buildingState = BuildingState.UnderConstruction;
                Debug.Log($"Building: {build.name} set state: {build.buildingState}");
            }
        }
    }

    private void CreateBuildStructureTask()
    {
        foreach (var building in listPlacedBuilding)
        {
            var buildingComponent = building.gameObject.GetComponent<Building>();
            buildingComponent.CreateBuildStructureTask();
        }
    }

    public void ConfirmPlacementAndStartBuild()
    {
        if (listPlacedBuilding.Count == 0) return;

        var totalRequiredResources = new Dictionary<ItemData, int>();

        foreach (var building in listPlacedBuilding)
        {
            var costs = building.BuildCosts;
            if (costs == null) continue;

            foreach (var cost in costs)
            {
                if (cost.itemData == null || cost.amount <= 0) continue;

                if (totalRequiredResources.ContainsKey(cost.itemData))
                    totalRequiredResources[cost.itemData] += cost.amount;
                else
                    totalRequiredResources[cost.itemData] = cost.amount;
            }
        }

        if (Inventory.Instance != null)
        {
            foreach (var kvp in totalRequiredResources)
            {
                var requiredItem = kvp.Key;
                var totalAmountNeeded = kvp.Value;

                if (Inventory.Instance.GetAmount(requiredItem) < totalAmountNeeded)
                {
                    Debug.LogWarning(
                        $"[EditBuildingManager] Không đủ tài nguyên tổng hợp! Cần tổng cộng {totalAmountNeeded}x {requiredItem.itemName}, nhưng kho chỉ có {Inventory.Instance.GetAmount(requiredItem)}.");

                    UINotificationManager.Instance.ShowNotification(
                        "Not enough resources to proceed with construction!", NotificationColorType.Error);
                    return;
                }
            }
        }
        else
        {
            Debug.LogError("[EditBuildingManager] Inventory.Instance bị null! Không thể đối chiếu tài nguyên.");
            return;
        }

        foreach (var building in listPlacedBuilding) building.ConsumeBuildResources();

        UpdateGraphNode();
        ChangeBuildingState();
        CreateBuildStructureTask();

        if (UnitManager.Instance != null)
            foreach (var building in listPlacedBuilding)
                if (!UnitManager.Instance.buildings.Contains(building))
                    UnitManager.Instance.buildings.Add(building);

        RollBackBuildingVisual();
        listPlacedBuilding.Clear();

        Debug.Log("[EditBuildingManager] Đã đối chiếu tổng chi phí, khấu trừ kho đồ và kích hoạt xây dựng thành công!");
    }

    public void CancelAllPlacedBuildings()
    {
        if (listPlacedBuilding.Count == 0) return;

        Debug.Log($"[EditBuildingManager] Bắt đầu hủy bỏ {listPlacedBuilding.Count} công trình đang đặt xem trước...");

        foreach (var building in listPlacedBuilding)
        {
            if (building == null) continue;

            var footprint = building.GetComponent<ObjectFootprint>();
            if (footprint != null)
            {
                var anchorCell = building.WorldToCell(building.transform.position, 1f);
                var cells = footprint.GetAbsoluteGridPositions(anchorCell);
                foreach (var cell in cells) occupiedCells.Remove(cell);
            }

            PoolManager.Instance.Despawn(building.gameObject);
        }

        listPlacedBuilding.Clear();
        Debug.Log("[EditBuildingManager] Đã dọn sạch móng tạm, khôi phục Grid về trạng thái trống hoàn toàn.");
    }

    public void ResetEditorManager()
    {
        Debug.Log("[EditBuildingManager] Đang dọn sạch toàn bộ dữ liệu hệ thống Editor...");

        if (listPlacedBuilding != null && listPlacedBuilding.Count > 0)
        {
            foreach (var building in listPlacedBuilding)
                if (building != null && building.gameObject != null)
                    PoolManager.Instance.Despawn(building.gameObject);

            listPlacedBuilding.Clear();
        }

        if (occupiedCells != null) occupiedCells.Clear();

        Debug.Log("[EditBuildingManager] Hệ thống Editor đã trống rỗng hoàn toàn, sẵn sàng hoạt động!");
    }

    public bool CheckIsTempBuiling(Building building)
    {
        if (listPlacedBuilding.Count > 0)
        {
            foreach (var placedBuilding in listPlacedBuilding)
            {
                if (placedBuilding == building)
                {
                    return true;
                }
            }
            return false;
        }
        return false;
    }
}