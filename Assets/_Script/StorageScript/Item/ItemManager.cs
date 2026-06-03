using System.Collections.Generic;
using _Script.ScriptableObjectScript;
using UnityEngine;

namespace _Script.ItemScript
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        [SerializeField] public List<Item> activeItems = new();

        [SerializeField] private List<Item> pendingItems = new();

        private float _clearPendingTimer;
        private const float ClearPendingInterval = 30f; 

        public List<Item> GetActiveItems() => activeItems;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Update()
        {
            _clearPendingTimer += Time.deltaTime;
            if (_clearPendingTimer >= ClearPendingInterval)
            {
                _clearPendingTimer = 0f;
                ReleasePendingItems();
            }
        }

        public void RegisterItem(Item item)
        {
            if (item != null && !activeItems.Contains(item) && !pendingItems.Contains(item)) activeItems.Add(item);
        }

        public void UnregisterItem(Item item)
        {
            if (item == null) return;
            if (activeItems.Contains(item)) activeItems.Remove(item);
            if (pendingItems.Contains(item)) pendingItems.Remove(item);
        }

        public void MoveToPending(Item item)
        {
            if (item == null) return;

            item.assignBuilder = null;

            if (activeItems.Contains(item)) activeItems.Remove(item);

            if (!pendingItems.Contains(item))
                pendingItems.Add(item);
        }

        public void ReleasePendingItems()
        {
            if (pendingItems.Count == 0) return;

            foreach (var item in pendingItems)
                if (item != null && item.gameObject.activeSelf)
                    activeItems.Add(item);

            pendingItems.Clear();
        }

        public Item FindNearestItem(Vector3 position, int layerIndex, Builder requestingBuilder)
        {
            Item nearestItem = null;
            var minDistance = Mathf.Infinity;

            for (var i = activeItems.Count - 1; i >= 0; i--)
            {
                var item = activeItems[i];

                if (item == null || item.gameObject == null)
                {
                    activeItems.RemoveAt(i);
                    continue;
                }

                if (item.layerIndex != layerIndex) continue;

                if (item.assignBuilder == null || item.assignBuilder == requestingBuilder)
                {
                    var distance = Vector2.Distance(position, item.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestItem = item;
                    }
                }
            }

            return nearestItem;
        }

        public void PopulateItemSaveData(GameSaveData saveData)
        {
            ReleasePendingItems();

            saveData.itemManagerSaveData.items.Clear();

            foreach (var item in activeItems)
            {
                if (item == null || item.itemData == null) continue;

                saveData.itemManagerSaveData.items.Add(new ItemSaveData
                {
                    itemDataId = item.itemData.id,
                    amount = item.amount,
                    layerIndex = item.layerIndex,
                    position = item.transform.position
                });
            }
        }

        public void LoadItemsFromSaveData(GameSaveData saveData)
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                if (activeItems[i] != null)
                {
                    PoolManager.Instance.Despawn(activeItems[i].gameObject);
                }
            }
            activeItems.Clear();

            for (int i = pendingItems.Count - 1; i >= 0; i--)
            {
                if (pendingItems[i] != null)
                {
                    PoolManager.Instance.Despawn(pendingItems[i].gameObject);
                }
            }
            pendingItems.Clear();

            if (saveData.itemManagerSaveData == null || saveData.itemManagerSaveData.items == null) return;

            foreach (var savedItem in saveData.itemManagerSaveData.items)
            {
                ItemData matchedItemData = SOManager.Instance.GetItemDataById(savedItem.itemDataId);
                if (matchedItemData == null) continue;

                GameObject itemPrefab = matchedItemData.itemPrefab;
                if (itemPrefab == null) continue;

                GameObject spawnedObj = PoolManager.Instance.Spawn(itemPrefab, savedItem.position, Quaternion.identity);
                if (spawnedObj != null)
                {
                    Item itemComp = spawnedObj.GetComponent<Item>();
                    if (itemComp != null)
                    {
                        itemComp.itemData = matchedItemData;
                        itemComp.amount = savedItem.amount;
                        itemComp.layerIndex = savedItem.layerIndex;
                        itemComp.assignBuilder = null; 

                        var customRender = spawnedObj.transform.Find("Custom Render Sprite");
                        if (customRender != null)
                        {
                            var crComp = customRender.GetComponent<CustomRender>();
                            if (crComp != null) crComp.layerIndex = itemComp.layerIndex;
                        }

                        RegisterItem(itemComp);
                    }
                }
            }
            Debug.Log($"[SaveLoadSystem] Đã khôi phục thành công {activeItems.Count} vật phẩm trên mặt đất.");
        }
    }
}