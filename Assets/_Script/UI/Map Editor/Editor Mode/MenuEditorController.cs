using System.Collections.Generic;
using UnityEngine;

public class MenuEditorController : MonoBehaviour
{
    public static MenuEditorController Instance;

    private List<MenuItem> menuItems = new List<MenuItem>();

    public GameObject cancelEditBuildingMode;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        RegisterAllMenuItem();
    }

    private void RegisterAllMenuItem()
    {
        var allMenuItems = FindObjectsOfType<MenuItem>(true);

        foreach (var item in allMenuItems)
        {
            menuItems.Add(item);
            item.OnMenuItemClicked -= HandleItemClicked;
            item.OnMenuItemClicked += HandleItemClicked;
        }
    }

    public void HandleItemClicked(BuildingData data)
    {
        if (cancelEditBuildingMode != null && !cancelEditBuildingMode.activeInHierarchy)
            cancelEditBuildingMode.SetActive(true);
    }

    public void CheckAndHideCancelButton()
    {
        if (cancelEditBuildingMode == null) return;

        var hasAnySelected = false;

        foreach (var item in menuItems)
        {
            if (item == null) continue;

            var children = item.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
                if (child.name == "Selected" && child.gameObject.activeInHierarchy)
                {
                    hasAnySelected = true;
                    break;
                }

            if (hasAnySelected) break;
        }

        if (!hasAnySelected)
        {
            cancelEditBuildingMode.SetActive(false);
            Debug.Log("[MenuEditor] Không còn MenuItem nào được chọn. Đã tự động ẩn panel Cancel!");
        }
    }

}
