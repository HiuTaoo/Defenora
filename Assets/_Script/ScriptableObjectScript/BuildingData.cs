using System.Collections.Generic;
using UnityEngine;

public abstract class BuildingData : ScriptableObject
{
    [Header("--- Base Identity ---")]
    public string buildingName;
    public BuildingType buildingType;

    [Header("--- Base Stats ---")]
    public float maxHealth = 100f;
    public int maxCapacity = 5;
    public float range = 5f;

    [Header("--- Resource Costs ---")] [Tooltip("Danh sách các tài nguyên và số lượng cần để XÂY DỰNG công trình")]
    public List<ResourceCost> buildCosts;

    [Tooltip("Danh sách các tài nguyên và số lượng cần để SỬA CHỮA công trình")]
    public List<ResourceCost> repairCosts;
}

