using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileItem : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tileBase;
    [SerializeField] private Image img;


    public void SelectTileItem()
    {
        DeSelectAllTileItem();
        MenuTilesController.Instance.targetTilemap = tilemap;
        MenuTilesController.Instance.selectedTile = tileBase;

        Transform[] children = GetComponentsInChildren<Transform>(true); 

        foreach (Transform child in children)
        {
            if (child.name == "Selected")
            {
                child.gameObject.SetActive(true);
            }
        }
        MenuTilesController.Instance.TogglePlaceTileMode();
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
