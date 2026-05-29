using UnityEngine;
using _Script.Enum; 

[System.Serializable]
public struct TrainingConfig
{
    public UnitType targetType;              
    public float trainingDurationInGameHours; 
    public GameObject unitPrefab;       
    
    [Header("--- Training Cost ---")]
    [Tooltip("Danh sách tài nguyên cần để huấn luyện đơn vị này")]
    public ResourceCost[] trainingCosts;
}

[CreateAssetMenu(fileName = "NewBarrackData", menuName = "Building/Barrack Data")]
public class BarrackData : BuildingData
{
    [Header("--- Barrack Training Config ---")]
    [Tooltip("Danh sách cấu hình các Class lính có thể huấn luyện tại đây")]
    public TrainingConfig[] upgradeConfigs;

    [Tooltip("Số lượng học viên tối đa có thể xếp hàng chờ cùng một lúc")]
    public int maxTraineeCapacity = 3;
}