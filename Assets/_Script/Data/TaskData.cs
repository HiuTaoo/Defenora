using _Script.Task;

namespace _Script.Data
{
    [System.Serializable]
    public class TaskData
    {
        public string id;
        public TaskType taskType;
        public int layerIndex;
        public TaskStatus taskStatus;
        public int maxBuilders;
        public float requiredProgress;
        public float currentProgress;
        public string targetGameObjectID; // Liên kết qua ID Object nếu cần, tạm thời lưu vị trí/tên nếu chưa có hệ thống ID toàn cục
    }
}