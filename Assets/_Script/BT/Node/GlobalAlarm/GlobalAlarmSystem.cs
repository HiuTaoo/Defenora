using System;
using UnityEngine;

namespace _Script.BT.GlobalAlarm
{
    public static class GlobalAlarmSystem
    {
        public static event Action<GameObject, Vector3, int> OnEnemySpotted;

        public static void TriggerAlarm(GameObject enemy, Vector3 spottedPosition, int layerIndex)
        {
            OnEnemySpotted?.Invoke(enemy, spottedPosition, layerIndex);
        }
    }
}