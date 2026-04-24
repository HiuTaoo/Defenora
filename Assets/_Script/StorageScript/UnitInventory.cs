using System.Collections.Generic;
using _Script.Resourse;
using _Script.Storage;
using UnityEngine;

public class UnitInventory : MonoBehaviour
{
    [Header("Unit Inventory Config")]
    public int maxCapacity = 10;
    
    [Header("Debug View")]
    [SerializeField] private List<InventoryEntry> debugItems = new List<InventoryEntry>();

    private Dictionary<ResourceType, int> items = new Dictionary<ResourceType, int>();

    public int CurrentCapacity
    {
        get
        {
            int total = 0;
            foreach (var amount in items.Values) total += amount;
            return total;
        }
    }

    public bool IsFull => CurrentCapacity >= maxCapacity;
    public bool IsEmpty => CurrentCapacity == 0;

    public int Add(ResourceType type, int amount)
    {
        if (amount <= 0) return 0;

        int spaceLeft = maxCapacity - CurrentCapacity;
        int addAmount = Mathf.Min(spaceLeft, amount);

        if (addAmount <= 0) return 0;

        if (!items.ContainsKey(type)) items[type] = 0;
        items[type] += addAmount;
        
        SyncDebugView();
        return addAmount;
    }

    public int Remove(ResourceType type, int amount)
    {
        if (!items.ContainsKey(type)) return 0;

        int removeAmount = Mathf.Min(items[type], amount);
        items[type] -= removeAmount;

        if (items[type] <= 0) items.Remove(type);
        
        SyncDebugView();
        return removeAmount;
    }

    public Dictionary<ResourceType, int> GetAll()
    {
        return new Dictionary<ResourceType, int>(items);
    }

    public void Clear()
    {
        items.Clear();
        SyncDebugView();
    }
    
    public bool TryGetMostAbundant(out ResourceType type)
    {
        type = ResourceType.None;

        if (items.Count == 0)
            return false;

        /*foreach (var pair in items)
        {
            Debug.Log($"Inventory contains: {pair.Key} x{pair.Value}");
        }*/

        int maxAmount = int.MinValue;

        foreach (var pair in items)
        {
            if (pair.Value > maxAmount)
            {
                maxAmount = pair.Value;
                type = pair.Key;
            }
        }

        //Debug.Log($"Most abundant selected: {type}");
        return true;
    }
    
    public bool TryTakeOneStack(out ResourceType type, out int amount)
    {
        foreach (var pair in items)
        {
            type = pair.Key;
            amount = pair.Value;
            return true;
        }

        type = default;
        amount = 0;
        return false;
    }

    private void SyncDebugView()
    {
        debugItems.Clear();
        foreach (var pair in items)
        {
            debugItems.Add(new InventoryEntry { type = pair.Key, amount = pair.Value });
        }
    }
}