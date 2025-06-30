using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private Dictionary<GameStateType, GameObject> stateUIs;
    private Dictionary<GameStateType, UIConfig> uiConfigs;

    public UIManager()
    {
        stateUIs = new Dictionary<GameStateType, GameObject>();
        uiConfigs = new Dictionary<GameStateType, UIConfig>();
    }

    // Register UI GameObject với config
    public void RegisterUI(GameStateType stateType, GameObject uiGameObject, UIConfig config = null)
    {
        stateUIs[stateType] = uiGameObject;
        uiConfigs[stateType] = config ?? new UIConfig();

        // Đảm bảo UI bắt đầu ở trạng thái ẩn
        uiGameObject.SetActive(false);
    }

    public void ShowUI(GameStateType stateType)
    {
        Debug.Log($"UIManager: Showing UI for {stateType}");

        // Ẩn tất cả UI trước
        HideAllUIs();

        // Hiển thị UI cho state hiện tại
        if (stateUIs.ContainsKey(stateType))
        {
            var ui = stateUIs[stateType];
            var config = uiConfigs[stateType];

            ui.SetActive(true);

            // Apply config nếu có
            ApplyUIConfig(ui, config);
        }
        else
        {
            Debug.LogWarning($"UIManager: No UI registered for state {stateType}");
        }
    }

    public void HideUI(GameStateType stateType)
    {
        if (stateUIs.ContainsKey(stateType))
        {
            stateUIs[stateType].SetActive(false);
        }
    }

    public void HideAllUIs()
    {
        foreach (var ui in stateUIs.Values)
        {
            ui.SetActive(false);
        }
    }

    private void ApplyUIConfig(GameObject ui, UIConfig config)
    {
        if (config.FadeIn)
        {
            // Có thể implement fade in animation
            var canvasGroup = ui.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                // Start fade in coroutine (simplified)
            }
        }

        if (config.Scale != Vector3.one)
        {
            ui.transform.localScale = config.Scale;
        }
    }

    // Utility methods
    public GameObject GetUI(GameStateType stateType)
    {
        return stateUIs.ContainsKey(stateType) ? stateUIs[stateType] : null;
    }

    public bool IsUIActive(GameStateType stateType)
    {
        return stateUIs.ContainsKey(stateType) && stateUIs[stateType].activeInHierarchy;
    }
}
