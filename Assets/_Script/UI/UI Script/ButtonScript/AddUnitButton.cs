using System;
using UnityEngine;

namespace _Script.UI.Button
{
    public class AddUnitButton: MonoBehaviour
    {
        private UnityEngine.UI.Button button;

        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(OnAddUnitClicked);
        }

        private void OnAddUnitClicked()
        {
            GameManager.Instance.OpenAvailableUnitGUI();
            UIManager.Instance.availableUnitPanel.ShowAvailableUnitInfo(
                SelectUnitSystem.Instance.targetBuilding.GetComponent<Building>());
        }
        
        
    }
}