using System;
using UnityEngine;

namespace _Script.UI.Button
{
    public class AddUnitButton: MonoBehaviour, IPoolable
    {
        private UnityEngine.UI.Button button;

        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
        }

        private void OnAddUnitClicked()
        {
            GameManager.Instance.OpenAvailableUnitGUI();
            UIManager.Instance.availableUnitPanel.ShowAvailableUnitInfo(
                SelectUnitSystem.Instance.targetBuilding.GetComponent<Building>());
        }

        public void OnSpawned()
        {
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            button.onClick.AddListener(OnAddUnitClicked);
        }

        public void OnDespawned()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}