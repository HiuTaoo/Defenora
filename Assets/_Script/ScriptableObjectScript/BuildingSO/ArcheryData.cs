namespace _Script.ScriptableObjectScript
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "NewArcheryData", menuName = "Building/Archery Data")]
    public class ArcheryData : BuildingData
    {
        [Header("--- Archery Training Config ---")]
        [Tooltip("Thời gian (tính bằng tiếng) để một NPC thông thường trở thành Cung thủ")]
        public float trainingDuration = 10f;
    
        [Tooltip("Số lượng học viên tối đa có thể học cùng một lúc")]
        public int maxTraineeCapacity = 3;
        
        [Header("--- Training Cost ---")]
        public ResourceCost[] trainingCosts;
    }
}