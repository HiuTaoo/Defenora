[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    public InventorySlot(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }

    public bool CanItemStack(ItemData item, int amountToAdd)
    {
        if (itemData != item) return false;
        return amount + amountToAdd <= itemData.maxStackSize;
    }
}