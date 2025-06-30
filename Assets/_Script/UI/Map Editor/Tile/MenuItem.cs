using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuItem : MonoBehaviour
{
    [Header("Build Prefab")]
    public Transform buildingPrefab;

    public System.Action<Transform> OnMenuItemClicked;

    void Start()
    {
        BuildingGhostPreviewSystem.Instance.RegisterMenuItem(this);
    }


    public void SelectItem()
    {
        DeSelectAllTileItem();

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "Selected")
            {
                child.gameObject.SetActive(true);
            }
        }

        // Gọi event và truyền prefab
        OnMenuItemClicked?.Invoke(buildingPrefab);
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
