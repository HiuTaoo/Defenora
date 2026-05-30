using System.Collections.Generic;
using UnityEngine;

namespace _Script.ItemScript
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        // Danh sách lưu trữ toàn bộ item đang nằm trên mặt đất
        [SerializeField] private List<Item> activeItems = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // Gọi hàm này khi Item được Spawn ra map từ Pool
        public void RegisterItem(Item item)
        {
            if (item != null && !activeItems.Contains(item)) activeItems.Add(item);
        }

        // Gọi hàm này khi Item bị Despawn hoặc được nhặt mất
        public void UnregisterItem(Item item)
        {
            if (item != null && activeItems.Contains(item)) activeItems.Remove(item);
        }

        /// <summary>
        ///     Tìm vật phẩm gần nhất với vị trí của Builder thuộc cùng một tầng (Layer Index)
        /// </summary>
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
    }
}