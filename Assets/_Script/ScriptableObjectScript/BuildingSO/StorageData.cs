using UnityEngine;

[CreateAssetMenu(fileName = "NewStorageData", menuName = "Building/Storage Data")]
public class StorageData : BuildingData
{
    [Header("--- Storage Specific ---")]
    [Tooltip("Sức chứa tối đa của kho tài nguyên này")]
    public int maxStorageCapacity = 100;
}