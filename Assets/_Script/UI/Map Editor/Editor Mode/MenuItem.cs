using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuItem : MonoBehaviour
{
    public System.Action<string> OnMenuItemClicked;

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

        SelectUnitSystem.Instance.isPlacing = true;
        MenuEditorController.Instance.cancelEditBuildingMode.SetActive(true);
        OnMenuItemClicked?.Invoke(gameObject.name);
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
