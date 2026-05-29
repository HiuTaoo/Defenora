using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShopData", menuName = "Shop/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("--- List of Available Units ---")]
    public List<ShopUnitEntry> availableUnits;

    [Header("--- List of Available Resources ---")]
    public List<ShopItemEntry> availableResources;
}