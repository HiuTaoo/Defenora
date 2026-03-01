using UnityEngine;

namespace _Script.BT.Node.BuilderNode.Idle
{
    public class WaitRandomTimeNode: BTActionNode
    {
        public  WaitRandomTimeNode(Builder builder): base(builder){}
        private float waitTime;
        private float timer;
        private bool initialized;

        private float minTime = 5f;
        private float maxTime = 15f;
        
        public override BTStatus Tick()
        {
            if (!initialized)
            {
                waitTime = Random.Range(minTime, maxTime);
                timer = 0f;
                initialized = true;
            }

            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                initialized = false;
                return BTStatus.Success;
            }

            return BTStatus.Running;
        }
    }
}