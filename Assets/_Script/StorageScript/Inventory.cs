using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Enum;
using _Script.Storage;
using _Script.Storage._Script.Storage;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    
    [Header("Debug View")]
    // Đồng bộ lại danh sách debug để hiển thị đúng cấu trúc InventoryEntry trong Inspector
    [SerializeField] private List<InventoryEntry> debugItems = new List<InventoryEntry>();

    private List<InventorySlot> _cachedTotalSlots = new List<InventorySlot>();
    private int _cachedMaxCapacity;
    private int _cachedCurrentCapacity;
    private bool _isDirty = true; 

    private List<Storage> _activeStorages = new List<Storage>();
    public event Action OnInventoryChanged;

    public int MaxCapacity { get { CheckRefresh(); return _cachedMaxCapacity; } }
    public int CurrentCapacity { get { CheckRefresh(); return _cachedCurrentCapacity; } }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        Invoke(nameof(RefreshStorageSubscriptions), 3.0f);
    }
    
    private void Update()
    {
        if (_isDirty)
        {
            CheckRefresh();
        }
    }

    public void RefreshStorageSubscriptions()
    {
        if (UnitManager.Instance == null)
        {
            return;
        }
    
        if (UnitManager.Instance.buildings == null)
        {
            return;
        }

        foreach (var s in _activeStorages) s.OnContentChanged -= SetDirty;

        _activeStorages = UnitManager.Instance.buildings
            .OfType<Storage>()
            .Where(b => b.buildingState == BuildingState.Completed)
            .ToList();

        foreach (var s in _activeStorages)
        {
            s.OnContentChanged += SetDirty;
            Debug.Log($"[Inventory-CHECK 3] Đã đăng ký thành công sự kiện cho kho: {s.gameObject.name}");
        }
    
        SetDirty();
    }

    private void SetDirty()
    {
        _isDirty = true;
    }

    private void CheckRefresh()
    {
        if (_isDirty) RebuildCache();
    }

    private void RebuildCache()
    {
        _cachedTotalSlots.Clear();
        _cachedMaxCapacity = 0;
        _cachedCurrentCapacity = 0;

        Dictionary<ItemData, int> combinedResourceAmounts = new Dictionary<ItemData, int>();

        foreach (var storage in _activeStorages)
        {
            if (storage == null) continue;

            _cachedMaxCapacity += storage.maxStorageCapacity;
    
            foreach (var slot in storage.GetAllSlots()) 
            {
                if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

                if (!combinedResourceAmounts.ContainsKey(slot.itemData))
                {
                    combinedResourceAmounts[slot.itemData] = 0;
                }
                combinedResourceAmounts[slot.itemData] += slot.amount;
                _cachedCurrentCapacity += slot.amount;
            }
        }

        foreach (var pair in combinedResourceAmounts)
        {
            ItemData itemData = pair.Key;
            int totalAmount = pair.Value;

            while (totalAmount > 0)
            {
                int amountInSlot = Mathf.Min(itemData.maxStackSize, totalAmount);
            
                _cachedTotalSlots.Add(new InventorySlot(itemData, amountInSlot));
            
                totalAmount -= amountInSlot;
            }
        }

        _isDirty = false;
        SyncDebugView();
    
        OnInventoryChanged?.Invoke(); 
    }

    public List<InventorySlot> GetAll()
    {
        CheckRefresh();
        return _cachedTotalSlots;
    }
    

    #region Core Methods - Điều phối Storage

    public int Add(ItemData itemData, int amount)
    {
        if (amount <= 0 || itemData == null) return 0;

        int remainingAmount = amount;

        foreach (var storage in _activeStorages)
        {
            if (remainingAmount <= 0) break;

            if (storage.CanStore(itemData, 1)) 
            {
                int added = storage.Add(itemData, remainingAmount);
                remainingAmount -= added;
            }
        }

        return amount - remainingAmount; 
    }

    public int Remove(ItemData itemData, int amount)
    {
        if (itemData == null) return 0;
        int remainingToTake = amount;

        foreach (var storage in _activeStorages)
        {
            if (remainingToTake <= 0) break;

            int amountInStorage = storage.GetAmount(itemData);
            if (amountInStorage > 0)
            {
                int removed = storage.Remove(itemData, remainingToTake);
                remainingToTake -= removed;
            }
        }

        return amount - remainingToTake; 
    }

    public int GetAmount(ItemData itemData)
    {
        if (itemData == null) return 0;
        return _activeStorages.Sum(s => s.GetAmount(itemData));
    }

    #endregion

    #region Advanced Query

    // Đổi out parameter 'type' từ ResourceType sang ItemData
    public bool TryTakeOneStack(out ItemData itemData, out int amount)
    {
        foreach (var storage in _activeStorages)
        {
            var itemsInStorage = storage.GetAllItems();
            foreach (var pair in itemsInStorage)
            {
                itemData = pair.Key;
                amount = pair.Value;
                return true;
            }
        }

        itemData = null;
        amount = 0;
        return false;
    }
    
    // Đổi out parameter 'type' từ ResourceType sang ItemData
    public bool TryGetMostAbundant(out ItemData itemData)
    {
        itemData = null;
    
        List<InventorySlot> allSlots = GetAll();

        if (allSlots == null || allSlots.Count == 0) return false;

        Dictionary<ItemData, int> combinedAmounts = new Dictionary<ItemData, int>();

        foreach (var slot in allSlots)
        {
            if (slot.itemData == null) continue;

            if (!combinedAmounts.ContainsKey(slot.itemData))
                combinedAmounts[slot.itemData] = 0;

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

    #endregion

    #region Debug Sync

    private void SyncDebugView()
    {
        debugItems.Clear();
    
        if (_cachedTotalSlots == null) return;

        foreach (var slot in _cachedTotalSlots)
        {
            if (slot.itemData == null) continue;

            debugItems.Add(new InventoryEntry
            {
                itemData = slot.itemData,
                amount = slot.amount
            });
        }
    }

    #endregion
}