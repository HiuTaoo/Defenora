using System.Collections;
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
        MenuItem[] allMenuItems = GameObject.FindObjectsOfType<MenuItem>();

        foreach (var item in allMenuItems)
        {
            if (item.gameObject.activeInHierarchy)
            {
                menuItems.Add(item);
                item.OnMenuItemClicked += HandleItemClicked;
            }
        }
    }

    public void HandleItemClicked(string prefab)
    {
        if(!cancelEditBuildingMode.activeInHierarchy)
            cancelEditBuildingMode.SetActive(true);
    }
}
