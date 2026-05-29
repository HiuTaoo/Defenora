using System.Collections.Generic;

[System.Serializable]
public class ShopSaveData
{
    public int lastRefreshedDay;
    public List<SavedShopUnitEntry> dailyUnits = new List<SavedShopUnitEntry>();
    public List<SavedShopItemEntry> dailyResources = new List<SavedShopItemEntry>();
}