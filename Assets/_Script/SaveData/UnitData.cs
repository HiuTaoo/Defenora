using System.Collections.Generic;
using _Script.Data;
using UnityEngine;

[System.Serializable]
public class UnitData
{
    public string id;
    public string unitName;
    public UnitType unitType;
    public int level;
    public int layerIndex;
    public float currentHealth;
    public Vector3 position;
    public string assignedBuilding;
    
    public List<SavedInventorySlot> backpackSlots = new List<SavedInventorySlot>();
}