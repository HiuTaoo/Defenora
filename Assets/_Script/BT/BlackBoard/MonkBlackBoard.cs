using UnityEngine;

namespace _Script.BT.BlackBoard
{
    public class MonkBlackBoard
    {
        public GameObject detectedEnemy;
        public GameObject lowHPAlly;
        public PathFinding pathFinding;
        public Vector2 lastDirection;
        public Vector3Int patrolTarget;
    }
}