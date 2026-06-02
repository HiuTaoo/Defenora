using _Script.Object_Pooling;
using UnityEngine;
using UnityEngine.UI;

public class BuildRequirementUI : MonoBehaviour
{
    [Header("--- UI Container Settings ---")]
    [Tooltip("Thanh chứa ô Grid (Có gắn component GridLayoutGroup hoặc LayoutGroup)")]
    [SerializeField]
    private Transform gridLayoutParent;

    [Header("--- Visibility ---")] [Tooltip("Panel chính bọc toàn bộ UI này để ẩn/hiện khi cần thiết")] [SerializeField]
    private GameObject mainPanel;

    private void Start()
    {
        HideUI();

        var allItems = FindObjectsOfType<MenuItem>(true);
        foreach (var item in allItems) item.OnMenuItemClicked += HandleMenuItemSelected;

        if (MenuEditorController.Instance != null && MenuEditorController.Instance.cancelEditBuildingMode != null)
        {
            var cancelBtn = MenuEditorController.Instance.cancelEditBuildingMode.GetComponent<Button>();
            if (cancelBtn != null) cancelBtn.onClick.AddListener(HideUI);
        }
    }

    private void Update()
    {
        if (BuildingGhostPreviewSystem.Instance != null && BuildingGhostPreviewSystem.Instance.currentGhost == null)
            if (mainPanel != null && mainPanel.activeSelf)
                HideUI();
    }

    private void OnDestroy()
    {
        var allItems = FindObjectsOfType<MenuItem>(true);
        foreach (var item in allItems)
            if (item != null)
                item.OnMenuItemClicked -= HandleMenuItemSelected;

        if (MenuEditorController.Instance != null && MenuEditorController.Instance.cancelEditBuildingMode != null)
        {
            var cancelBtn = MenuEditorController.Instance.cancelEditBuildingMode.GetComponent<Button>();
            if (cancelBtn != null) cancelBtn.onClick.RemoveListener(HideUI);
        }
    }

    /// <summary>
    ///     Xử lý sự kiện khi một MenuItem được bấm chọn
    /// </summary>
    private void HandleMenuItemSelected(BuildingData buildingData)
    {
        ClearCurrentSlots();

        if (buildingData == null || buildingData.buildCosts == null || buildingData.buildCosts.Count == 0)
        {
            HideUI();
            return;
        }

        if (mainPanel != null) mainPanel.SetActive(true);

        foreach (var cost in buildingData.buildCosts)
        {
            if (cost.itemData == null || cost.amount <= 0) continue;

            var slotObj = PoolManager.Instance.Spawn(
                PrefabConfig.Instance.inventorySlotPrefab,
                gridLayoutParent.position,
                Quaternion.identity
            );

            if (slotObj != null)
            {
                slotObj.transform.SetParent(gridLayoutParent, false);
                slotObj.transform.localScale = Vector3.one;
                slotObj.transform.SetAsLastSibling();

                var resourceSlotScript = slotObj.GetComponent<UIResourceSlot>();
                if (resourceSlotScript != null) resourceSlotScript.Setup(cost.itemData, cost.amount);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayoutParent.GetComponent<RectTransform>());
    }

    /// <summary>
    ///     Thu hồi toàn bộ các ô UIResourceSlot đang hiển thị trên Grid trả về ngầm trong PoolManager
    /// </summary>
    public void ClearCurrentSlots()
    {
        if (gridLayoutParent == null) return;

        for (var i = gridLayoutParent.childCount - 1; i >= 0; i--)
        {
            var child = gridLayoutParent.GetChild(i);

            if (child.gameObject.activeSelf) PoolManager.Instance.Despawn(child.gameObject);
        }
    }

    public void HideUI()
    {
        ClearCurrentSlots();

        if (mainPanel != null) mainPanel.SetActive(false);
    }
}