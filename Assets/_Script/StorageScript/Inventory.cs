using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Resourse;
using _Script.Storage;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    [Header("Debug View")]
    [SerializeField] private List<InventoryEntry> debugItems = new List<InventoryEntry>();

    private Dictionary<ResourceType, int> _cachedTotalItems = new Dictionary<ResourceType, int>();
    private int _cachedMaxCapacity;
    private int _cachedCurrentCapacity;
    private bool _isDirty = true; 

    private List<Storage> _activeStorages = new List<Storage>();

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
        _cachedTotalItems.Clear();
        _cachedMaxCapacity = 0;
        _cachedCurrentCapacity = 0;

        foreach (var storage in _activeStorages)
        {
            _cachedMaxCapacity += storage.maxStoreageCapacity;
            
            foreach (var pair in storage.GetAllItems())
            {
                if (!_cachedTotalItems.ContainsKey(pair.Key))
                    _cachedTotalItems[pair.Key] = 0;
                
                _cachedTotalItems[pair.Key] += pair.Value;
                _cachedCurrentCapacity += pair.Value;
            }
        }

        _isDirty = false;
        SyncDebugView();
        Debug.Log("[Inventory] Cache đã được làm mới!");
    }

    public Dictionary<ResourceType, int> GetAll()
    {
        CheckRefresh();
        return new Dictionary<ResourceType, int>(_cachedTotalItems);
    }
    

    #region Core Methods - Điều phối Storage

    public int Add(ResourceType type, int amount)
    {
        if (amount <= 0) return 0;

        int remainingAmount = amount;

        foreach (var storage in _activeStorages)
        {
            if (remainingAmount <= 0) break;

            if (storage.CanStore(type, 1)) 
            {
                int added = storage.Add(type, remainingAmount);
                remainingAmount -= added;
            }
        }

        return amount - remainingAmount; 
    }

    public int Remove(ResourceType type, int amount)
    {
        int remainingToTake = amount;

        foreach (var storage in _activeStorages)
        {
            if (remainingToTake <= 0) break;

            int amountInStorage = storage.GetAmount(type);
            if (amountInStorage > 0)
            {
                int removed = storage.Remove(type, remainingToTake);
                remainingToTake -= removed;
            }
        }

        return amount - remainingToTake; 
    }

    public int GetAmount(ResourceType type)
    {
        return _activeStorages.Sum(s => s.GetAmount(type));
    }

    #endregion

    #region Advanced Query

    public bool TryTakeOneStack(out ResourceType type, out int amount)
    {
        foreach (var storage in _activeStorages)
        {
            var itemsInStorage = storage.GetAllItems();
            foreach (var pair in itemsInStorage)
            {
                type = pair.Key;
                amount = pair.Value;
                return true;
            }
        }

        type = default;
        amount = 0;
        return false;
    }
    
    public bool TryGetMostAbundant(out ResourceType type)
    {
        type = ResourceType.None;
        var allItems = GetAll();

        if (allItems.Count == 0) return false;

        int maxAmount = int.MinValue;

        foreach (var pair in allItems)
        {
            if (pair.Value > maxAmount)
            {
                maxAmount = pair.Value;
                type = pair.Key;
            }
        }

        return true;
    }

    #endregion

    #region Debug Sync

    private void SyncDebugView()
    {
        debugItems.Clear();
        
        foreach (var pair in _cachedTotalItems)
        {
            debugItems.Add(new InventoryEntry
            {
                type = pair.Key,
                amount = pair.Value
            });
        }
    }

    #endregion
}