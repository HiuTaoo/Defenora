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

    [Header("--- Resource Costs (Wood) ---")]
    [Tooltip("Lượng Wood cần để xây dựng công trình")]
    public int buildWoodCost;
    
    [Tooltip("Lượng Wood cần để sửa chữa công trình từ 0% -> 100% máu")]
    public int repairWoodCost;
}

