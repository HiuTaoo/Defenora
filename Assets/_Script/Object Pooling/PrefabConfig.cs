using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace _Script.Object_Pooling
{
    public class PrefabConfig : MonoBehaviour
    {
        public static PrefabConfig Instance;

        [Header("Unit Prefab")]
        public GameObject archerPrefab;
        public GameObject monkPrefab;
        public GameObject warriorPrefab;
        public GameObject builderPrefab;
        public GameObject lancerPrefab;
        public GameObject civilianPrefab;
        
        [Header("Enemy Prefab")]
        public GameObject torchGoblinPrefab;
        public GameObject tntGoblinPrefab;
        public GameObject barrelPrefab;

        [Header("Building Prefab")]
        public GameObject fortressPrefab;
        public GameObject watchTowerPrefab;
        public GameObject storagePrefab;
        public GameObject archeryPrefab;
        public GameObject barrackPrefab;
        public GameObject monasteryPrefab;

        [Header("Item Prefab")]
        public GameObject woodPrefab;
        public GameObject meatPrefab;
        public GameObject goldBagPrefab;
        public GameObject arrowPrefab;
        public GameObject dynamitePrefab;
        
        [Header("GUI Prefab")]
        public GameObject addUnitButtonPrefab;
        public GameObject unitIconPrefab;
        public GameObject statDetailGUIPrefab;
        public GameObject inventorySlotPrefab;
        public GameObject trainingQueueSlotUIPrefab;
        public GameObject shopItemSlotPrefab;
        public GameObject toastItemPrefab;

        [Header("Effects Prefab")] public GameObject healEffectPrefab;

        [Header("Tree Prefab")]
        public GameObject[] treePrefabs;

        [Header("Rock Prefab")]
        public GameObject[] rockPrefabs;

        [Header("Bush Prefab")]
        public GameObject[] bushPrefabs;

        [Header("Animal Prefab")]
        public GameObject[] animalPrefabs;
        


        private Dictionary<string, GameObject> prefabLookup 
            = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            BuildLookup();
        }

        private void BuildLookup()
        {
            var fields = typeof(PrefabConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
                if (field.FieldType == typeof(GameObject))
                {
                    var prefab = field.GetValue(this) as GameObject;
                    AddPrefab(prefab);
                }
                else if (field.FieldType == typeof(GameObject[]))
                {
                    var prefabArray = field.GetValue(this) as GameObject[];
                    AddPrefabArray(prefabArray);
                }

            Debug.Log(
                $"[PrefabConfig] 🚀 Tự động hóa quét hoàn tất! Đã nạp {prefabLookup.Count} Prefabs vào Dictionary.");
        }

        private void AddPrefab(GameObject prefab)
        {
            if (prefab == null) return;

            if (!prefabLookup.ContainsKey(prefab.name))
                prefabLookup.Add(prefab.name, prefab);
        }

        private void AddPrefabArray(GameObject[] prefabs)
        {
            if (prefabs == null) return;

            foreach (var prefab in prefabs)
            {
                AddPrefab(prefab);
            }
        }

        public GameObject GetPrefab(string prefabName)
        {
            if (prefabLookup.TryGetValue(prefabName, out GameObject prefab))
            {
                return prefab;
            }

            Debug.LogWarning($"Prefab '{prefabName}' not found!");
            return null;
        }
    }
}