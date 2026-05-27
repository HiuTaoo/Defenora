using System.Collections.Generic;
using _Script.Object_Pooling;
using UnityEngine;

public class UIInventoryManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform gridContainer;

    private List<UIResourceSlot> _spawnedSlots = new List<UIResourceSlot>();

    private void Start()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += UpdateInventoryUI;
        }
        UpdateInventoryUI();
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
    }

    public void UpdateInventoryUI()
    {
        if (Inventory.Instance == null) return;

        foreach (var slot in _spawnedSlots)
        {
            if (slot != null && slot.gameObject != null) 
            {
                slot.gameObject.transform.SetParent(null); 
                PoolManager.Instance.Despawn(slot.gameObject);
            }
        }
        _spawnedSlots.Clear();

        List<InventorySlot> allSlots = Inventory.Instance.GetAll();

        foreach (var slotData in allSlots)
        {
            if (slotData == null || slotData.itemData == null || slotData.amount <= 0) 
                continue;

            GameObject go = PoolManager.Instance.Spawn(
                PrefabConfig.Instance.inventorySlotPrefab, 
                gridContainer.position, 
                Quaternion.identity
            );
        
            go.transform.SetParent(gridContainer, false);

            UIResourceSlot newSlot = go.GetComponent<UIResourceSlot>();
            if (newSlot != null)
            {
                newSlot.Setup(slotData.itemData, slotData.amount); 
                _spawnedSlots.Add(newSlot);
            }
        }
    }
}