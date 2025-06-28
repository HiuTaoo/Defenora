using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MenuItem : MonoBehaviour
{
    private Image img;

    private void Awake()
    {
        img = transform.Find("Building").GetComponent<Image>();
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
        MenuTilesController.Instance.mouseIndicator.GetComponent<Image>().sprite = img.sprite;

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
