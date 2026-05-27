using System;
using System.Collections.Generic;
using _Script.Resourse;
using _Script.Unit_Management_System.Building;
using UnityEngine;

[System.Serializable]
public struct StorageEntry
{
    public ItemData itemData;
    public int amount;
}

public class Storage : Building, IStorage
{
    [Header("Storage Config")]
    public int maxStoreageCapacity = 100;

    [Header("Debug View (Read Only)")]
    [SerializeField] private List<StorageEntry> debugStorage = new List<StorageEntry>();

    // XÓA BỎ HOÀN TOÀN Dictionary storage cũ tại đây!
    private List<InventorySlot> storageSlots = new List<InventorySlot>();
    
    public event Action OnContentChanged;

    private void Start()
    {
        buildingType = BuildingType.Storage;
    }

    public int CurrentCapacity
    {
        get
        {
            // Sửa đổi: Tính toán dung lượng dựa hoàn toàn trên danh sách Slots mới
            int total = 0;
            foreach (var slot in storageSlots)
            {
                if (slot != null) total += slot.amount;
            }
            return total;
        }
    }

    public bool CanStore(ItemData itemData, int amount)
    {
        return CurrentCapacity + amount <= maxStoreageCapacity;
    }

    public int Add(ItemData itemData, int amount)
    {
        if (amount <= 0 || itemData == null) return 0;

        int spaceLeft = maxStoreageCapacity - CurrentCapacity;
        int amountToDistribute = Mathf.Min(spaceLeft, amount);
        int totalAdded = amountToDistribute;

        if (amountToDistribute <= 0) return 0;

        // 1. Tìm kiếm slot cùng loại tài nguyên còn trống chỗ để gom tụ lại
        for (int i = 0; i < storageSlots.Count; i++)
        {
            if (amountToDistribute <= 0) break;

            var slot = storageSlots[i];
            if (slot.itemData == itemData && slot.amount < itemData.maxStackSize)
            {
                int remainSpaceInSlot = itemData.maxStackSize - slot.amount;
                int toAdd = Mathf.Min(remainSpaceInSlot, amountToDistribute);

                slot.amount += toAdd;
                amountToDistribute -= toAdd;
            }
        }

        // 2. Nếu vượt ngưỡng maxStackSize hoặc chưa có slot nào, tạo slot tách biệt mới
        while (amountToDistribute > 0)
        {
            int toAdd = Mathf.Min(itemData.maxStackSize, amountToDistribute);
            storageSlots.Add(new InventorySlot(itemData, toAdd));
            amountToDistribute -= toAdd;
        }

        OnContentChanged?.Invoke();
        SyncDebugView();
    
        return totalAdded;
    }

    public int Remove(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return 0;

        int remainingToTake = amount;
        int totalRemoved = 0;

        // Trừ cuốn chiếu từ slot cuối cùng trở về đầu để tối ưu dọn dẹp danh sách
        for (int i = storageSlots.Count - 1; i >= 0; i--)
        {
            if (remainingToTake <= 0) break;

            var slot = storageSlots[i];
            if (slot.itemData == itemData)
            {
                int toTake = Mathf.Min(slot.amount, remainingToTake);
                slot.amount -= toTake;
                remainingToTake -= toTake;
                totalRemoved += toTake;

                if (slot.amount <= 0)
                {
                    storageSlots.RemoveAt(i);
                }
            }
        }

        if (totalRemoved > 0)
        {
            OnContentChanged?.Invoke();
            SyncDebugView();
        }

        return totalRemoved;
    }

    private void DestroyStorage()
    {
        if (storageSlots != null)
        {
            storageSlots.Clear();            
            SyncDebugView();            
            OnContentChanged?.Invoke(); 
        }
    }
    
    public List<InventorySlot> GetAllSlots()
    {
        return storageSlots;
    }

    // Cập nhật Interface cũ để giữ an toàn cho luồng dữ liệu liên quan
    public Dictionary<ItemData, int> GetAllItems()
    {
        Dictionary<ItemData, int> combined = new Dictionary<ItemData, int>();
        foreach(var slot in storageSlots)
        {
            if(!combined.ContainsKey(slot.itemData)) combined[slot.itemData] = 0;
            combined[slot.itemData] += slot.amount;
        }
        return combined;
    }

    public int GetAmount(ItemData itemData)
    {
        int total = 0;
        foreach (var slot in storageSlots)
        {
            if (slot.itemData == itemData) total += slot.amount;
        }
        return total;
    }

    protected override void OnUnitAdded(Unit unit)
    {
        GetComponent<GuardComponent>()?.OnUnitAdded(unit);
    }

    protected override void OnUnitRemoved(Unit unit)
    {
        GetComponent<GuardComponent>()?.OnUnitRemoved(unit);
    }

    protected override void HandleDeath()
    {
        base.HandleDeath();
        DestroyStorage();
    }

    #region Debug Sync

    private void SyncDebugView()
    {
        debugStorage.Clear();
        foreach (var slot in storageSlots)
        {
            debugStorage.Add(new StorageEntry
            {
                itemData = slot.itemData,
                amount = slot.amount
            });
        }
    }

    #endregion
}