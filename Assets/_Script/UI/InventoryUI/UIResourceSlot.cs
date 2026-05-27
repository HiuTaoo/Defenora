using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIResourceSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private ItemData _currentItemData;

    public void Setup(ItemData itemData, int amount)
    {
        if (itemData == null) return;

        _currentItemData = itemData;
        
        if (iconImage != null)
        {
            iconImage.sprite = itemData.icon;
        }

        UpdateAmount(amount);
    }

    public void UpdateAmount(int amount)
    {
        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }
    }
}