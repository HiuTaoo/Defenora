using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string id;              
    public string itemName;    
    public ResourceType resourceType;
    
    [TextArea(2, 5)]
    public string description;     
    
    [Header("Visuals")]
    public Sprite icon;  
    public GameObject itemPrefab;

    [Header("Properties")]
    public int maxStackSize = 99;  
    public bool isResource = true; 
}