using System;
using UnityEngine;

namespace _Script.BT.GlobalAlarm
{
    public static class GlobalAlarmSystem
    {
        public static event Action<GameObject, Vector3> OnEnemySpotted;

        public static void TriggerAlarm(GameObject enemy, Vector3 spottedPosition)
        {
            OnEnemySpotted?.Invoke(enemy, spottedPosition);
        }
    }
}