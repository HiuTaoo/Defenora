using System.Collections.Generic;
using _Script.Resourse;
using UnityEngine;

[System.Serializable]
public struct StorageEntry
{
    public ResourceType type;
    public int amount;
}

public class Storage : Building, IStorage
{
    [Header("Storage Config")]
    public int maxStoreageCapacity = 100;

    [Header("Debug View (Read Only)")]
    [SerializeField] private List<StorageEntry> debugStorage = new List<StorageEntry>();

    private Dictionary<ResourceType, int> storage
        = new Dictionary<ResourceType, int>();

    private void Start()
    {
        buildingType = BuildingType.Storage;
    }

    public int CurrentCapacity
    {
        get
        {
            int total = 0;
            foreach (var item in storage)
                total += item.Value;
            return total;
        }
    }

    public bool CanStore(ResourceType type, int amount)
    {
        return CurrentCapacity + amount <= maxStoreageCapacity;
    }

    public int Add(ResourceType type, int amount)
    {
        if (amount <= 0) return 0;

        int spaceLeft = maxStoreageCapacity - CurrentCapacity;
        int addAmount = Mathf.Min(spaceLeft, amount);

        if (addAmount <= 0) return 0;

        if (!storage.ContainsKey(type))
            storage[type] = 0;

        storage[type] += addAmount;

        SyncDebugView();
        return addAmount;
    }

    public int Remove(ResourceType type, int amount)
    {
        if (!storage.ContainsKey(type)) return 0;

        int removeAmount = Mathf.Min(storage[type], amount);
        storage[type] -= removeAmount;

        if (storage[type] <= 0)
            storage.Remove(type);

        SyncDebugView();
        return removeAmount;
    }

    public int GetAmount(ResourceType type)
    {
        return storage.TryGetValue(type, out int value) ? value : 0;
    }

    #region Debug Sync

    private void SyncDebugView()
    {
        debugStorage.Clear();

        foreach (var pair in storage)
        {
            debugStorage.Add(new StorageEntry
            {
                type = pair.Key,
                amount = pair.Value
            });
        }
    }

    #endregion
}