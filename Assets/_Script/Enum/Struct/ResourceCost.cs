using UnityEngine;

[System.Serializable]
public struct ResourceCost
{
    [Tooltip("Loại tài nguyên cần tiêu hao")]
    public ItemData itemData;
    
    [Tooltip("Số lượng cần")]
    public int amount;
}