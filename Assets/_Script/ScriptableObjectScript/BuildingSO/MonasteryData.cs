using UnityEngine;

[CreateAssetMenu(fileName = "NewMonasteryData", menuName = "Building/Monastery Data")]
public class MonasteryData : BuildingData
{
    [Header("--- Monastery Training Config ---")]
    [Tooltip("Thời gian (tính bằng GIỜ TRONG GAME) để một Civilian đắc đạo thành Monk.")]
    public float trainingDurationInGameHours = 4f;

    [Tooltip("Số lượng học viên (Civilian) tối đa có thể tu hành cùng một lúc")]
    public int maxTraineeCapacity = 2;
}