using System;
using System.Collections.Generic;
using System.Linq;
using _Script.Resourse;
using _Script.Storage;
using UnityEngine;

    public class Inventory : MonoBehaviour
    {
        public int maxCapacity;
        [Header("Debug View (Read Only)")]
        [SerializeField] private List<InventoryEntry> debugItems = new List<InventoryEntry>();

        private Dictionary<ResourceType, int> items
            = new Dictionary<ResourceType, int>();

        public int MaxCapacity => UnitManager.Instance.buildings
            .Where(b => b.buildingType == BuildingType.Storage) 
            .Sum(b => b.maxCapacity);

        public int CurrentCapacity
        {
            get
            {
                int total = 0;
                foreach (var pair in items)
                    total += pair.Value;
                return total;
            }
        }

        public bool IsFull => CurrentCapacity >= MaxCapacity;
        public bool IsEmpty => CurrentCapacity == 0;

        private void Update()
        {
            maxCapacity = MaxCapacity;
        }

        #region Core Methods

        public int Add(ResourceType type, int amount)
        {
            if (amount <= 0) return 0;

            int spaceLeft = MaxCapacity - CurrentCapacity;
            int addAmount = Mathf.Min(spaceLeft, amount);

            if (addAmount <= 0) return 0;

            if (!items.ContainsKey(type))
                items[type] = 0;

            items[type] += addAmount;

            SyncDebugView();
            return addAmount;
        }

        public int Remove(ResourceType type, int amount)
        {
            if (!items.ContainsKey(type)) return 0;

            int removeAmount = Mathf.Min(items[type], amount);
            items[type] -= removeAmount;

            if (items[type] <= 0)
                items.Remove(type);

            SyncDebugView();
            return removeAmount;
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

        public int GetAmount(ResourceType type)
        {
            return items.TryGetValue(type, out int value) ? value : 0;
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

        #endregion

        #region Debug Sync

        private void SyncDebugView()
        {
            debugItems.Clear();

            foreach (var pair in items)
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