using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    public Dictionary<GameStateType, Dictionary<string, GameObject>> stateUIs;
    private Dictionary<GameStateType, UIConfig> stateConfigs;
    private Dictionary<string, UIConfig> individualUIConfigs;

    public UIManager()
    {
        stateUIs = new Dictionary<GameStateType, Dictionary<string, GameObject>>();
        stateConfigs = new Dictionary<GameStateType, UIConfig>();
        individualUIConfigs = new Dictionary<string, UIConfig>();
    }

    /// <summary>
    /// Đăng ký một UI cho một state cụ thể
    /// </summary>
    /// <param name="stateType">Loại game state</param>
    /// <param name="uiName">Tên UI (string để linh hoạt hơn)</param>
    /// <param name="uiGameObject">GameObject của UI</param>
    /// <param name="config">Config riêng cho UI này (tùy chọn)</param>
    public void RegisterUI(GameStateType stateType, string uiName, GameObject uiGameObject, UIConfig config = null)
    {
        if (!stateUIs.ContainsKey(stateType))
            stateUIs[stateType] = new Dictionary<string, GameObject>();

        stateUIs[stateType][uiName] = uiGameObject;

        if (config != null)
            individualUIConfigs[GetUIKey(stateType, uiName)] = config;

        uiGameObject.SetActive(false);

        //Debug.Log($"UIManager: Registered UI '{uiName}' for state {stateType}");
    }

    /// <summary>
    /// Đăng ký config chung cho toàn bộ state
    /// </summary>
    public void RegisterStateConfig(GameStateType stateType, UIConfig config)
    {
        stateConfigs[stateType] = config;
    }

    /// <summary>
    /// Hiển thị tất cả UI của một state
    /// </summary>
    public void ShowStateUI(GameStateType stateType)
    {
        HideAllUIs(stateType);

        if (stateUIs.ContainsKey(stateType))
        {
            var uiDict = stateUIs[stateType];
            var stateConfig = stateConfigs.ContainsKey(stateType) ? stateConfigs[stateType] : null;

            foreach (var kvp in uiDict)
            {
                string uiName = kvp.Key;
                GameObject uiGameObject = kvp.Value;

                uiGameObject.SetActive(true);

                UIConfig configToApply = GetUIConfig(stateType, uiName) ?? stateConfig;
                if (configToApply != null)
                    ApplyUIConfig(uiGameObject, configToApply);
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// Hiển thị một UI cụ thể trong state
    /// </summary>
    public void ShowUI(GameStateType stateType, string uiName)
    {
        if (stateUIs.ContainsKey(stateType) && stateUIs[stateType].ContainsKey(uiName))
        {
            GameObject uiGameObject = stateUIs[stateType][uiName];
            uiGameObject.SetActive(true);

            UIConfig config = GetUIConfig(stateType, uiName) ??
                            (stateConfigs.ContainsKey(stateType) ? stateConfigs[stateType] : null);

            if (config != null)
                ApplyUIConfig(uiGameObject, config);

        }
        else
        {
        }
    }

    /// <summary>
    /// Ẩn một UI cụ thể trong state
    /// </summary>
    public void HideUI(GameStateType stateType, string uiName)
    {
        if (stateUIs.ContainsKey(stateType) && stateUIs[stateType].ContainsKey(uiName))
        {
            stateUIs[stateType][uiName].SetActive(false);
        }
    }

    /// <summary>
    /// Ẩn tất cả UI của một state
    /// </summary>
    public void HideStateUI(GameStateType stateType)
    {
        if (stateUIs.ContainsKey(stateType))
        {
            foreach (var kvp in stateUIs[stateType])
            {
                kvp.Value.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ẩn tất cả UI của tất cả state
    /// </summary>
    /// 
    public void HideAllUIs()
    {
        foreach (var stateDict in stateUIs.Values)
        {
            foreach (var uiGameObject in stateDict.Values)
            {
                uiGameObject.SetActive(false);
            }
        }
    }

    public void HideAllUIs(GameStateType gameStateType)
    {
        if (stateUIs.ContainsKey(gameStateType))
        {
            foreach (var uiGameObject in stateUIs[gameStateType].Values)
            {
                uiGameObject.SetActive(false);
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// Lấy GameObject của một UI cụ thể
    /// </summary>
    public GameObject GetUI(GameStateType stateType, string uiName)
    {
        if (stateUIs.ContainsKey(stateType) && stateUIs[stateType].ContainsKey(uiName))
            return stateUIs[stateType][uiName];
        return null;
    }

    /// <summary>
    /// Lấy tất cả UI của một state
    /// </summary>
    public Dictionary<string, GameObject> GetStateUIs(GameStateType stateType)
    {
        return stateUIs.ContainsKey(stateType) ? stateUIs[stateType] : new Dictionary<string, GameObject>();
    }

    /// <summary>
    /// Kiểm tra một UI có đang active không
    /// </summary>
    public bool IsUIActive(GameStateType stateType, string uiName)
    {
        GameObject ui = GetUI(stateType, uiName);
        return ui != null && ui.activeInHierarchy;
    }

    /// <summary>
    /// Kiểm tra có UI nào của state đang active không
    /// </summary>
    public bool IsAnyUIActive(GameStateType stateType)
    {
        if (!stateUIs.ContainsKey(stateType)) return false;

        foreach (var ui in stateUIs[stateType].Values)
        {
            if (ui.activeInHierarchy) return true;
        }
        return false;
    }

    /// <summary>
    /// Lấy danh sách tên UI của một state
    /// </summary>
    public List<string> GetUINames(GameStateType stateType)
    {
        if (stateUIs.ContainsKey(stateType))
            return new List<string>(stateUIs[stateType].Keys);
        return new List<string>();
    }

    private void ApplyUIConfig(GameObject ui, UIConfig config)
    {
        if (config.FadeIn)
        {
            var canvasGroup = ui.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                // Có thể thêm tween fade in ở đây
            }
        }

        if (config.Scale != Vector3.one)
        {
            ui.transform.localScale = config.Scale;
        }

        // Có thể thêm các config khác như position, rotation, etc.
    }

    private UIConfig GetUIConfig(GameStateType stateType, string uiName)
    {
        string key = GetUIKey(stateType, uiName);
        return individualUIConfigs.ContainsKey(key) ? individualUIConfigs[key] : null;
    }

    private string GetUIKey(GameStateType stateType, string uiName)
    {
        return $"{stateType}_{uiName}";
    }


}


// Class để định nghĩa tên UI constants (tùy chọn, để tránh typo)
public static class UINames
{
    // Editor UI
    public const string EditorMenu = "EditorMenu";
    public const string SelectUnitGUI = "SelectUnitGUI";
    public const string BuildingPanel = "BuildingPanel";

    // Gameplay UI
    public const string GameplayHUD = "GameplayHUD";
    public const string PauseMenu = "PauseMenu";
    public const string Inventory = "InventoryPanel";

    // Menu UI
    public const string MainMenu = "MainMenu";
    public const string SettingsMenu = "SettingsMenu";

    //Playing UI
    public const string InteractButton = "InteractButton";

    // Common UI
    public const string LoadingScreen = "LoadingScreen";
    public const string DialogPanel = "DialogPanel";
}
