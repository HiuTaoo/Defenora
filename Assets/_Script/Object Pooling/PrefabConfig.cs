using System.Collections.Generic;
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
        
        [Header("Enemy Prefab")]
        public GameObject torchGoblinPrefab;
        public GameObject tntGoblinPrefab;
        public GameObject barrelPrefab;

        [Header("Building Prefab")]
        public GameObject fortressPrefab;
        public GameObject watchTowerPrefab;
        public GameObject storagePrefab;

        [Header("Item Prefab")]
        public GameObject woodPrefab;
        public GameObject arrowPrefab;
        public GameObject dynamitePrefab;

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
            AddPrefab(archerPrefab);
            AddPrefab(monkPrefab);
            AddPrefab(warriorPrefab);
            AddPrefab(builderPrefab);
            AddPrefab(lancerPrefab);
            
            AddPrefab(torchGoblinPrefab);
            AddPrefab(tntGoblinPrefab);
            AddPrefab(barrelPrefab);

            AddPrefab(fortressPrefab);
            AddPrefab(watchTowerPrefab);
            AddPrefab(storagePrefab);

            AddPrefab(woodPrefab);

            AddPrefabArray(treePrefabs);
            AddPrefabArray(rockPrefabs);
            AddPrefabArray(bushPrefabs);
            AddPrefabArray(animalPrefabs);
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