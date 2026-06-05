using System.Collections.Generic;
using _Script.Storage;
using _Script.Storage._Script.Storage;
using UnityEngine;

public class UnitInventory : MonoBehaviour
{
    [Header("Unit Inventory Config")]
    public int maxCapacity = 10;
    
    [Header("Debug View")]
    [SerializeField] private List<InventoryEntry> debugItems = new List<InventoryEntry>();

    private List<InventorySlot> items = new List<InventorySlot>();

    public int CurrentCapacity
    {
        get
        {
            int total = 0;
            foreach (var slot in items) total += slot.amount;
            return total;
        }
    }

    public bool IsFull => CurrentCapacity >= maxCapacity;
    public bool IsEmpty => CurrentCapacity == 0;

    public int Add(ItemData itemData, int amount)
    {
        if (amount <= 0 || itemData == null) return 0;

        int spaceLeft = maxCapacity - CurrentCapacity;
        int amountToDistribute = Mathf.Min(spaceLeft, amount);
        int totalAdded = amountToDistribute;

        if (amountToDistribute <= 0) return 0;

        // 1. Tìm ô có sẵn trong balo để dồn tụ tài nguyên
        for (int i = 0; i < items.Count; i++)
        {
            if (amountToDistribute <= 0) break;

            var slot = items[i];
            if (slot.itemData == itemData && slot.amount < itemData.maxStackSize)
            {
                int remainSpace = itemData.maxStackSize - slot.amount;
                int toAdd = Mathf.Min(remainSpace, amountToDistribute);

                slot.amount += toAdd;
                amountToDistribute -= toAdd;
            }
        }

        // 2. Tạo ô mới khi vượt ngưỡng stack size của item
        while (amountToDistribute > 0)
        {
            int toAdd = Mathf.Min(itemData.maxStackSize, amountToDistribute);
            items.Add(new InventorySlot(itemData, toAdd));
            amountToDistribute -= toAdd;
        }
        
        SyncDebugView();
        return totalAdded;
    }

    public int Remove(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return 0;

        int remainingToTake = amount;
        int totalRemoved = 0;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (remainingToTake <= 0) break;

            var slot = items[i];
            if (slot.itemData == itemData)
            {
                int toTake = Mathf.Min(slot.amount, remainingToTake);
                slot.amount -= toTake;
                remainingToTake -= toTake;
                totalRemoved += toTake;

                if (slot.amount <= 0) items.RemoveAt(i);
            }
        }
        
        if (totalRemoved > 0) SyncDebugView();
        return totalRemoved;
    }

    public List<InventorySlot> GetAll()
    {
        return items;
    }

    public void Clear()
    {
        items.Clear();
        SyncDebugView();
    }
    
    public bool TryGetMostAbundant(out ItemData itemData)
    {
        itemData = null;
        if (items.Count == 0) return false;

        Dictionary<ItemData, int> combinedAmounts = new Dictionary<ItemData, int>();
        foreach (var slot in items)
        {
            if (slot.itemData == null) continue;
            if (!combinedAmounts.ContainsKey(slot.itemData)) combinedAmounts[slot.itemData] = 0;
            combinedAmounts[slot.itemData] += slot.amount;
        }

        int maxAmount = int.MinValue;
        foreach (var pair in combinedAmounts)
        {
            if (pair.Value > maxAmount)
            {
                maxAmount = pair.Value;
                itemData = pair.Key;
            }
        }

        return itemData != null;
    }
    
    public bool TryTakeOneStack(out ItemData itemData, out int amount)
    {
        if (items.Count > 0)
        {
            var slot = items[0];
            itemData = slot.itemData;
            amount = slot.amount;
            return true;
        }

        itemData = null;
        amount = 0;
        return false;
    }

    private void SyncDebugView()
    {
        debugItems.Clear();
        foreach (var slot in items)
        {
            debugItems.Add(new InventoryEntry { itemData = slot.itemData, amount = slot.amount });
        }
    }
}