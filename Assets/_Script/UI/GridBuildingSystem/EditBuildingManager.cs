using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Rendering.ShadowCascadeGUI;

/// <summary>
/// Quản lý grid, cell đã chiếm.
/// </summary>
public class EditBuildingManager : MonoBehaviour
{
    public static EditBuildingManager Instance;
    public HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    public List<Building> listPlacedBuilding = new List<Building>();

    private GraphNode graphNode;
    private SaveLoadSystem saveLoadSystem;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        graphNode = FindObjectOfType<GraphNode>();
        saveLoadSystem = FindObjectOfType<SaveLoadSystem>();

        if (graphNode == null)
            Debug.LogError("GraphNode không tồn tại trên scene!");

        if (saveLoadSystem != null)
            saveLoadSystem.OnSave += HandleSaveBuilding;


    }

    /// <summary>
    /// Kiểm tra ô đã chiếm.
    /// </summary>
    public bool IsCellOccupied(Vector2Int cellPos)
    {
        return occupiedCells.Contains(cellPos);
    }

    /// <summary>
    /// Đánh dấu ô là đã chiếm.
    /// </summary>
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

        Color color =  new Color(0, 1, 0, 0.75f);
        var renderer = currentbuilding.GetComponent<SpriteRenderer>();
        renderer.color = color;
        
        currentbuilding.transform.name = $"{building.buildingType}: {System.Guid.NewGuid()}";
        Debug.Log($"Building: {building.name} is in Placing State");

        listPlacedBuilding.Add(building);
        //building.UpdateRenderSortingOrder(LayerManager.Instance.layerIndex);

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
            else
            {
                // ➜ Ô ngoài cùng thì vẫn kiểm tra node walkable, nhưng cho phép occupied
                // Không có gì phải làm ở đây
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

        return new Vector3(
            (cellPos.x + 0.5f) * cellSize,
            (cellPos.y + 0.5f) * cellSize,
            0f
        );
    }

    public void RollBackBuildingVisual()
    {
        foreach(var building in listPlacedBuilding) {
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
        if(listPlacedBuilding != null)
        {
            foreach(var build in listPlacedBuilding)
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

    private void HandleSaveBuilding() {
        UpdateGraphNode();
        ChangeBuildingState();
        CreateBuildStructureTask();
        UnitManager.Instance.buildings.AddRange(listPlacedBuilding);
        RollBackBuildingVisual();
        listPlacedBuilding.Clear();
    }
    
    public bool CheckIsTempBuiling(Building building)
    {
        if(listPlacedBuilding.Count > 0)
        {
            foreach(var placedBuilding in  listPlacedBuilding)
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
