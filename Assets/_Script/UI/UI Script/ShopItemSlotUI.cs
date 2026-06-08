using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour, IPoolable
{
    [Header("--- UI Elements Reference ---")]
    [SerializeField] private Image itemIconImage;       
    [SerializeField] private TextMeshProUGUI priceText; 
    [SerializeField] private Button buyButton;         

    private int _currentPrice;
    private ItemData _itemData;
    private GameObject _unitPrefab; 

    private Action<ShopItemSlotUI> _onBuyCallback;

    public int CurrentPrice => _currentPrice;
    public ItemData ItemData => _itemData;
    public GameObject UnitPrefab => _unitPrefab;

    /// <summary>
    /// Setup slot dành cho loại bán VẬT PHẨM / TÀI NGUYÊN (Sử dụng ItemData)
    /// </summary>
    public void SetupAsItem(ItemData itemData, int price, Action<ShopItemSlotUI> onBuyClick)
    {
        if (itemData == null) return;

        _itemData = itemData;
        _unitPrefab = null;
        _currentPrice = price;
        _onBuyCallback = onBuyClick;

        if (itemIconImage != null) itemIconImage.sprite = itemData.icon;
        if (priceText != null) priceText.text = price.ToString();

        RegisterButtonEvent();
    }

    /// <summary>
    /// Setup slot dành cho loại bán UNIT / LÍNH / CÔNG TRÌNH (Sử dụng Prefab hoặc Sprite trực tiếp)
    /// </summary>
    public void SetupAsUnit(GameObject unitPrefab, Sprite unitIcon, int price, Action<ShopItemSlotUI> onBuyClick)
    {
        if (unitPrefab == null) return;

        _itemData = null;
        _unitPrefab = unitPrefab;
        _currentPrice = price;
        _onBuyCallback = onBuyClick;

        if (itemIconImage != null && unitIcon != null) itemIconImage.sprite = unitIcon;
        if (priceText != null) priceText.text = price.ToString();

        RegisterButtonEvent();
    }

    private void RegisterButtonEvent()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnSlotClicked()
    {
        _onBuyCallback?.Invoke(this);
    }

    /// <summary>
    /// Được gọi ngay khi ô Shop này được lôi ra khỏi Pool
    /// </summary>
    public void OnSpawned()
    {
        if (buyButton == null) 
            buyButton = GetComponent<Button>();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            
            buyButton.interactable = true;
            
            buyButton.onClick.AddListener(OnSlotClicked);
        }

        _currentPrice = 0;
        _itemData = null;
        _unitPrefab = null;
        _onBuyCallback = null;

        if (itemIconImage != null) 
            itemIconImage.sprite = null;

        if (priceText != null) 
            priceText.text = string.Empty;
    }

    /// <summary>
    /// Được gọi ngay trước khi ô Shop này bị thu hồi về ngầm trong Pool[cite: 5]
    /// </summary>
    public void OnDespawned()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
        }

        _itemData = null;
        _unitPrefab = null;
        _onBuyCallback = null;
    }
}