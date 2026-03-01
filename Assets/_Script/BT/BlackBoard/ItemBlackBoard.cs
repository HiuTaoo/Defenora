using System;
using UnityEngine;

namespace _Script.BT.BlackBoard
{
    public class ItemBlackBoard: MonoBehaviour
    {
        public static ItemBlackBoard Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject woodPrefab;
    }
}