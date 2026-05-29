[System.Serializable]
public class DailyShopEntry
{
    public bool isUnit;               
    public ShopUnitEntry unitData;    
    public ShopItemEntry resourceData;
    public int currentStock;          

    // Constructor cho Unit
    public DailyShopEntry(ShopUnitEntry unit, int stock)
    {
        this.isUnit = true;
        this.unitData = unit;
        this.currentStock = stock;
    }

    // Constructor cho Resource
    public DailyShopEntry(ShopItemEntry resource, int stock)
    {
        this.isUnit = false;
        this.resourceData = resource;
        this.currentStock = stock;
    }
}