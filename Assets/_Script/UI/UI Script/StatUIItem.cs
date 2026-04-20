using TMPro;
using UnityEngine;

namespace _Script.UI.UI_Script
{
    public class StatUIItem : MonoBehaviour
    {
        public TextMeshProUGUI statNameText;
        public TextMeshProUGUI statValueText;

        public void Setup(string statName, string statValue)
        {
            if(statNameText != null) statNameText.text = statName;
            if(statValueText != null) statValueText.text = statValue;
        }
    }
}