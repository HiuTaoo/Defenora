using System.Collections.Generic;
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
        public string targetGameObjectID;

        public List<string> assignedBuilderIDs = new List<string>();
    }
}