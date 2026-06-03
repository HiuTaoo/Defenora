using System;
using UnityEngine;

public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    public static event Action<int> OnCoinChanged;

    [SerializeField] private int currentCoins;
    public int CurrentCoins => currentCoins;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        currentCoins += amount;
        OnCoinChanged?.Invoke(currentCoins);
        Debug.Log($"[Wallet] Bạn nhận được {amount} coin. Tổng: {currentCoins}");
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            OnCoinChanged?.Invoke(currentCoins);
            Debug.Log($"[Wallet] Bạn đã tiêu {amount} coin. Còn lại: {currentCoins}");
            return true;
        }
        
        Debug.LogWarning("[Wallet] Không đủ coin để thực hiện giao dịch!");
        return false;
    }

    public void ForceSpendCoins(int amount)
    {
        currentCoins -= amount;
        OnCoinChanged?.Invoke(currentCoins);
        Debug.Log($"[Wallet] Bạn đã tiêu {amount} coin. Còn lại: {currentCoins}");
    }

    // Hàm bổ sung phục vụ cho Save/Load
    public void SetCoinsOnLoad(int amount)
    {
        currentCoins = amount;
        OnCoinChanged?.Invoke(currentCoins);
    }
}