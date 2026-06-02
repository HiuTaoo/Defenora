using System;
using UnityEngine;

public class MenuItem : MonoBehaviour
{
    [Header("--- Building Config ---")] [SerializeField]
    private BuildingData buildingConfig;

    public Action<BuildingData> OnMenuItemClicked;

    public BuildingData BuildingConfig => buildingConfig;

    void Start()
    {
        if (BuildingGhostPreviewSystem.Instance != null) BuildingGhostPreviewSystem.Instance.RegisterMenuItem(this);
    }

    public void SelectItem()
    {
        if (buildingConfig == null)
        {
            Debug.LogError($"[MenuItem] {gameObject.name} chưa được gán file BuildingData trong Inspector!");
            return;
        }

        DeSelectAllTileItem();

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == "Selected")
            {
                child.gameObject.SetActive(true);
            }
        }

        if (SelectUnitSystem.Instance != null) SelectUnitSystem.Instance.isPlacing = true;

        if (MenuEditorController.Instance != null && MenuEditorController.Instance.cancelEditBuildingMode != null)
            MenuEditorController.Instance.cancelEditBuildingMode.SetActive(true);

        OnMenuItemClicked?.Invoke(buildingConfig);
    }

    public void DeSelectAllTileItem()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Selected")
            {
                obj.SetActive(false);
            }
        }
    }
}