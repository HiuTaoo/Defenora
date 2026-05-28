using System;
using UnityEngine;

namespace _Script.ScriptableObjectScript
{
    public class SOManager: MonoBehaviour
    {
        public static SOManager Instance;
        
        [Header("Inventory Data Registry")]
        public ItemData[] allItemDataSO;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        public ItemData GetItemDataById(string id)
        {
            foreach (var itemSO in allItemDataSO)
            {
                if (itemSO != null && itemSO.id == id) return itemSO;
            }
            Debug.LogError($"[Inventory Load] Không tìm thấy ItemData SO nào có ID: {id}");
            return null;
        }
    }
}