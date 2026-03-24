using UnityEngine;

namespace _Script.BT.BlackBoard
{
    public class WarriorBlackBoard
    {
        public GameObject detectedEnemy;
        public PathFinding pathFinding;
        public Vector2 lastDirection;
        public Vector3Int patrolTarget;
    }
}