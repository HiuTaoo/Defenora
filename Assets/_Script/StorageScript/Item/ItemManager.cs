// Mở file ItemManager.cs và cập nhật lại như sau:

using System.Collections.Generic;
using UnityEngine;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript; // Thêm namespace pooling nếu cần

namespace _Script.ItemScript
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        [SerializeField] private List<Item> activeItems = new();

        // 🌟 THÊM: Cho phép SaveLoadSystem truy cập danh sách
        public List<Item> GetActiveItems() => activeItems;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void RegisterItem(Item item)
        {
            if (item != null && !activeItems.Contains(item)) activeItems.Add(item);
        }

        public void UnregisterItem(Item item)
        {
            if (item != null && activeItems.Contains(item)) activeItems.Remove(item);
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