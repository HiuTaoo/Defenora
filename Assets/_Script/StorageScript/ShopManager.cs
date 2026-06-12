using System.Collections.Generic;
using System.Linq;
using _Script.Object_Pooling;
using _Script.ScriptableObjectScript;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("--- Shop Data Configuration ---")] [SerializeField]
    private ShopData shopConfiguration;

    [Header("--- UI Content Panels Reference ---")] [SerializeField]
    private RectTransform unitContentPanel;

    [SerializeField] private RectTransform resourceContentPanel;

    [Header("--- Shop Mode Settings ---")]
    [Tooltip("Nếu true: Bật chế độ Daily (Random số lượng slot cố định, mua xong tự ẩn ô, tự làm mới qua ngày).\nNếu false: Hiện toàn bộ danh sách cấu hình, mua vô hạn.")]
    [SerializeField]
    private bool isDailyMode;

    [Tooltip("Số lượng ô hiển thị ngẫu nhiên cho mỗi nhóm khi bật Daily Mode")] [SerializeField]
    private int slotsPerGroup = 8;

    private List<ShopUnitEntry> dailyUnits = new List<ShopUnitEntry>();
    private List<ShopItemEntry> dailyResources = new List<ShopItemEntry>();

    private List<GameObject> spawnedSlotObjects = new List<GameObject>();
    private int lastRefreshedDay = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeShopUI();

        if (isDailyMode && TimeOfDaySystem.Instance != null)
        {
            TimeOfDaySystem.Instance.OnDayChanged += HandleDayChanged;
            Debug.Log("[ShopManager] 🟢 Đã kết nối Event OnDayChanged thành công để tự động reset Shop!");
        }
    }

    private void OnDestroy()
    {
        if (TimeOfDaySystem.Instance != null) TimeOfDaySystem.Instance.OnDayChanged -= HandleDayChanged;
    }


    /// <summary>
    ///     Hàm xử lý tự động làm mới hàng hóa khi nhận được tín hiệu qua ngày mới từ Event
    /// </summary>
    private void HandleDayChanged(int newDay)
    {
        if (!isDailyMode) return;

        if (newDay != lastRefreshedDay)
        {
            lastRefreshedDay = newDay;
            GenerateDailyItems();
            BuildShopUI();
            Debug.Log(
                $"[ShopManager] 🏪 [Event] Đã bước sang ngày mới {newDay}! Tiến hành reset và đổi danh sách hàng hóa.");
        }
    }

    /// <summary>
    /// Hàm khởi tạo chính của cửa hàng (Gồm cả check mode để xử lý dữ liệu đầu vào)
    /// </summary>
    public void InitializeShopUI()
    {
        if (shopConfiguration == null)
        {
            Debug.LogError("[ShopManager] Chưa gán file ShopData cấu hình cho Shop!");
            return;
        }

        if (isDailyMode)
        {
            if (TimeOfDaySystem.Instance != null && lastRefreshedDay == -1)
                lastRefreshedDay = TimeOfDaySystem.Instance.CurrentDay;

            if (dailyUnits.Count == 0 && dailyResources.Count == 0)
            {
                GenerateDailyItems();
            }
        }

        BuildShopUI();
    }

    /// <summary>
    /// Hàm xử lý bốc ngẫu nhiên các phần tử (Chỉ chạy ở Daily Mode)
    /// </summary>
    public void GenerateDailyItems()
    {
        dailyUnits.Clear();
        dailyResources.Clear();

        if (shopConfiguration.availableUnits != null && shopConfiguration.availableUnits.Count > 0)
        {
            for (int i = 0; i < slotsPerGroup; i++)
            {
                int randomIndex = Random.Range(0, shopConfiguration.availableUnits.Count);
                dailyUnits.Add(shopConfiguration.availableUnits[randomIndex]);
            }
        }

        if (shopConfiguration.availableResources != null && shopConfiguration.availableResources.Count > 0)
        {
            for (int i = 0; i < slotsPerGroup; i++)
            {
                int randomIndex = Random.Range(0, shopConfiguration.availableResources.Count);
                dailyResources.Add(shopConfiguration.availableResources[randomIndex]);
            }
        }
    }

    /// <summary>
    /// Hàm trung tâm chịu trách nhiệm sinh Prefab Slot ra màn hình dựa theo Mode hiện tại
    /// </summary>
    private void BuildShopUI()
    {
        ClearShopUI();

        if (isDailyMode)
        {
            foreach (var unitEntry in dailyUnits)
            {
                SpawnUnitSlot(unitEntry);
            }
        }
        else
        {
            if (shopConfiguration.availableUnits != null)
            {
                foreach (var unitEntry in shopConfiguration.availableUnits)
                {
                    if (unitEntry.unitPrefab == null) continue;
                    SpawnUnitSlot(unitEntry);
                }
            }
        }

        if (isDailyMode)
        {
            foreach (var itemEntry in dailyResources)
            {
                SpawnResourceSlot(itemEntry);
            }
        }
        else
        {
            if (shopConfiguration.availableResources != null)
            {
                foreach (var itemEntry in shopConfiguration.availableResources)
                {
                    if (itemEntry.itemData == null) continue;
                    SpawnResourceSlot(itemEntry);
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(unitContentPanel);
        LayoutRebuilder.ForceRebuildLayoutImmediate(resourceContentPanel);
    }

    private void SpawnUnitSlot(ShopUnitEntry unitEntry)
    {
        var slotObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.shopItemSlotPrefab, unitContentPanel.transform.position, Quaternion.identity);
        slotObj.transform.SetParent(unitContentPanel);
        slotObj.transform.localScale = Vector3.one;
        slotObj.gameObject.SetActive(true);
        spawnedSlotObjects.Add(slotObj);

        var slotUI = slotObj.GetComponent<ShopItemSlotUI>();
        if (slotUI != null) slotUI.SetupAsUnit(unitEntry.unitPrefab, unitEntry.unitIcon, unitEntry.price, HandleBuyItem);
    }

    private void SpawnResourceSlot(ShopItemEntry itemEntry)
    {
        var slotObj = PoolManager.Instance.Spawn(PrefabConfig.Instance.shopItemSlotPrefab, resourceContentPanel.transform.position, Quaternion.identity);
        slotObj.transform.SetParent(resourceContentPanel);
        slotObj.transform.localScale = Vector3.one;
        slotObj.gameObject.SetActive(true);
        spawnedSlotObjects.Add(slotObj);

        var slotUI = slotObj.GetComponent<ShopItemSlotUI>();
        if (slotUI != null) slotUI.SetupAsItem(itemEntry.itemData, itemEntry.price, HandleBuyItem);
    }

    /// <summary>
    /// Xử lý mua vật phẩm
    /// </summary>
    private void HandleBuyItem(ShopItemSlotUI clickedSlot)
    {
        if (WalletManager.Instance == null) return;

        // 1. KIỂM TRA ĐIỀU KIỆN KINH TẾ TRƯỚC (Không trừ tiền ngay)
        if (!WalletManager.Instance.CanSpendCoins(clickedSlot.CurrentPrice))
        {
            // Thông báo không đủ tiền (Đã được WalletManager.TrySpendCoins xử lý cảnh báo, 
            // nhưng ở đây ta dùng CanSpendCoins nên cần kích hoạt cảnh báo thủ công nếu muốn,
            // hoặc gọi TrySpendCoins với giá trị 0/để Wallet tự báo nếu CanSpendCoins trả về false)
            UINotificationManager.Instance.ShowNotification("Not enough coins in the wallet to make this transaction!",
                NotificationColorType.Warning);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundNames.SfxWarning);
            return;
        }

        var isPurchaseSuccessful = false; // Cờ đánh dấu giao dịch thành công

        // =================================================================
        // TRƯỜNG HỢP A: MUA VẬT PHẨM (RESOURCE / ITEM)
        // =================================================================
        if (clickedSlot.ItemData != null)
        {
            // Thử thêm vào kho (Hàm Add trả về int? hoặc int tùy theo bản fix trước)
            var quantity = Inventory.Instance.Add(clickedSlot.ItemData, 1);

            // Nếu hệ thống trả về null (không có kho) hoặc số lượng thêm được bằng 0 (kho đầy)
            if (quantity == null || quantity == 0)
            {
                UINotificationManager.Instance.ShowNotification(
                    "There are currently no storage to store items, build an storage !", NotificationColorType.Warning);
                return; // Hủy mua, không trừ tiền!
            }

            // Đã nhét vào kho thành công -> Xác nhận mua
            isPurchaseSuccessful = true;
            Debug.Log($"[Shop] Mua thành công 1x {clickedSlot.ItemData.name}");

            if (isDailyMode)
            {
                var match = dailyResources
                    .Cast<ShopItemEntry?>()
                    .FirstOrDefault(r => r.Value.itemData == clickedSlot.ItemData);

                if (match != null) dailyResources.Remove(match.Value);
            }
        }
        // =================================================================
        // TRƯỜNG HỢP B: MUA ĐƠN VỊ LÍNH (UNIT)
        // =================================================================
        else if (clickedSlot.UnitPrefab != null)
        {
            var playerGO = PlayerController.Instance;
            var playerPosition = Vector3.zero;
            var foundPlayer = false;

            if (playerGO != null)
            {
                playerPosition = playerGO.transform.position;
                playerPosition.z = 0f;
                foundPlayer = true;
            }
            else
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    playerPosition = camera.transform.position;
                    playerPosition.z = 0f;
                    foundPlayer = true;
                }
            }

            if (foundPlayer)
            {
                Building closestBuilding = null;
                var closestDistance = float.MaxValue;

                if (UnitManager.Instance != null && UnitManager.Instance.buildings != null)
                    foreach (var building in UnitManager.Instance.buildings)
                    {
                        if (building == null) continue;
                        var distance = (building.transform.position - playerPosition).sqrMagnitude;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestBuilding = building;
                        }
                    }

                var spawnPosition = playerPosition;
                var targetLayerIndex = 0;

                if (closestBuilding != null)
                {
                    spawnPosition = closestBuilding.GetRandomPositionAroundBuilding();
                    targetLayerIndex = closestBuilding.LayerIndex;
                }
                else
                {
                    var randomOffset = Random.insideUnitSphere * 1.5f;
                    randomOffset.z = 0f;
                    spawnPosition += randomOffset;
                    var playerComp = playerGO;
                    if (playerComp != null && playerComp.characterMovement != null)
                        targetLayerIndex = playerComp.characterMovement.CurrentLayer;
                }

                // Tiến hành Spawn lính từ Pool
                var spawnedUnitObj =
                    PoolManager.Instance.Spawn(clickedSlot.UnitPrefab, spawnPosition, Quaternion.identity);

                if (spawnedUnitObj != null)
                {
                    var unitComponent = spawnedUnitObj.GetComponent<Unit>();
                    if (unitComponent != null)
                    {
                        if (unitComponent.floorAgent == null)
                            unitComponent.floorAgent = unitComponent.GetComponentInChildren<FloorAgent>();

                        if (unitComponent.characterMovement == null)
                            unitComponent.characterMovement = unitComponent.GetComponentInChildren<CharacterMovement>();

                        if (unitComponent.characterMovement != null)
                            unitComponent.characterMovement.CurrentLayer = targetLayerIndex;
                        if (unitComponent.floorAgent != null) unitComponent.floorAgent.MoveToFloor(targetLayerIndex);

                        if (UnitManager.Instance != null)
                            UnitManager.Instance.RegisterUnit(unitComponent);

                        // Đã gọi lính ra sàn đấu thành công -> Xác nhận mua
                        isPurchaseSuccessful = true;
                        Debug.Log($"[Shop] Mua và xuất kích thành công Unit: {clickedSlot.UnitPrefab.name}");

                        if (isDailyMode)
                        {
                            var match = dailyResources
                                .Cast<ShopItemEntry?>()
                                .FirstOrDefault(r => r.Value.itemData == clickedSlot.ItemData);

                            if (match != null) dailyResources.Remove(match.Value);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError(
                    "[Shop] Không thể mua Unit vì không tìm thấy cả Player lẫn Main Camera để lấy vị trí gốc!");
                return;
            }
        }

        if (isPurchaseSuccessful)
        {
            WalletManager.Instance.ForceSpendCoins(clickedSlot.CurrentPrice);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SoundNames.SfxPaySuccess);

            if (isDailyMode)
            {
                spawnedSlotObjects.Remove(clickedSlot.gameObject);
                PoolManager.Instance.Despawn(clickedSlot.gameObject);

                LayoutRebuilder.ForceRebuildLayoutImmediate(unitContentPanel);
                LayoutRebuilder.ForceRebuildLayoutImmediate(resourceContentPanel);
            }
        }
    }

    public void ClearShopUI()
    {
        foreach (var obj in spawnedSlotObjects)
        {
            if (obj != null)
            {
                PoolManager.Instance.Despawn(obj);
            }
        }
        spawnedSlotObjects.Clear();
    }

    public int GetResourcePrice(ItemData item)
    {
        if (shopConfiguration == null || item == null) return 0;

        var matchedItem = shopConfiguration.availableResources.Find(r => r.itemData == item);

        return matchedItem.itemData != null ? matchedItem.price : 0;
    }

#region Save/Load Logic

public void PopulateShopSaveData(GameSaveData saveData)
{
    saveData.shopSaveData.lastRefreshedDay = lastRefreshedDay;
    saveData.shopSaveData.dailyUnits.Clear();
    saveData.shopSaveData.dailyResources.Clear();

    foreach (var unit in dailyUnits)
    {
        if (unit.unitPrefab == null) continue;
        saveData.shopSaveData.dailyUnits.Add(new SavedShopUnitEntry
        {
            unitPrefabName = unit.unitPrefab.name,
            price = unit.price
        });
    }

    foreach (var res in dailyResources)
    {
        if (res.itemData == null) continue;
        saveData.shopSaveData.dailyResources.Add(new SavedShopItemEntry
        {
            itemID = res.itemData.id,
            price = res.price
        });
    }
}

public void LoadShopFromSaveData(GameSaveData saveData)
{
    if (!isDailyMode || saveData.shopSaveData == null) return;

    lastRefreshedDay = saveData.shopSaveData.lastRefreshedDay;
    dailyUnits.Clear();
    dailyResources.Clear();

    foreach (var savedUnit in saveData.shopSaveData.dailyUnits)
    {
        var matchedConfig = shopConfiguration.availableUnits
            .Find(u => u.unitPrefab != null && u.unitPrefab.name == savedUnit.unitPrefabName);

        if (matchedConfig.unitPrefab != null)
            dailyUnits.Add(new ShopUnitEntry
            {
                unitName = matchedConfig.unitName,
                unitPrefab = matchedConfig.unitPrefab,
                unitIcon = matchedConfig.unitIcon,
                price = savedUnit.price
            });
    }

    foreach (var savedRes in saveData.shopSaveData.dailyResources)
    {
        var matchedItem = SOManager.Instance.GetItemDataById(savedRes.itemID);
        if (matchedItem != null)
            dailyResources.Add(new ShopItemEntry
            {
                itemData = matchedItem,
                price = savedRes.price
            });
    }

    BuildShopUI();
}

#endregion
}