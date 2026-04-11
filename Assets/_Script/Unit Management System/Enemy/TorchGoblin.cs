using System;
using Unity.VisualScripting;

namespace _Script.Unit_Management_System.Enemy
{
    public class TorchGoblin: Enemy
    {
        public int layerIndex = 0;

        private void Update()
        {
            floorAgent.MoveToFloor(layerIndex);
            characterMovement.CurrentLayer =  layerIndex;
        }
    }
}