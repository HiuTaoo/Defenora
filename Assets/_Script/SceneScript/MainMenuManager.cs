using System;
using System.Collections;
using System.IO;
using _Script.UI.UI_Script;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cần thiết để dùng Coroutine
// Cần thiết để dùng UI Slider

public class MainMenuManager : MonoBehaviour
{
    [Header("Cài đặt Transition Trắng")]
    [Tooltip("Canvas dùng riêng cho hiệu ứng che màn hình")]
    [SerializeField] private RectTransform transitionCanvas;
    
    [Tooltip("Panel Trắng bên Trái")]
    [SerializeField] private RectTransform leftWhitePanel;
    
    [Tooltip("Panel Trắng bên Phải")]
    [SerializeField] private RectTransform rightWhitePanel;
    
    [Tooltip("Thời gian trượt vào (giây)")]
    [SerializeField] private float transitionDuration = 0.5f;
    
    [Tooltip("Tên Scene tiếp theo muốn load")]
    [SerializeField] private int nextSceneIndex = 1;

    [Header("Cài đặt Thanh Tiến Độ (Loading)")]
    [Tooltip("Object cha chứa toàn bộ UI Loading (Thanh Slider + Chữ Loading)")]
    [SerializeField] private GameObject loadingGroup;
    
    [Tooltip("Thanh trượt Loading")]
    [SerializeField] private Slider progressBar;
    
    [Tooltip("Chữ hiển thị phần trăm (100%)")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("UI Khác")]
    [Tooltip("Chữ 'Click to Start' (sẽ bị tắt khi bấm)")]
    [SerializeField] private GameObject hideUI;

    [Header("Confirm Dialog")] public ConfirmDialog confirmDialog;

    private bool isTransitioning = false;

    private void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        confirmDialog = GetComponentInChildren<ConfirmDialog>();
    }
    
    private void Start()
    {
        if (loadingGroup != null)
        {
            loadingGroup.SetActive(false);
        }

        if (PoolManager.Instance != null)
            PoolManager.Instance.ClearAndRefillPools();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI() && !isTransitioning)
            {
                if (hideUI != null) hideUI.gameObject.SetActive(false);
                StartWhiteWipeTransition();
            }
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void StartWhiteWipeTransition()
    {
        isTransitioning = true;

        float halfScreenWidth = transitionCanvas.rect.width / 2f;
        float overlapOffset = 20f; 
        
        float targetLeft = halfScreenWidth - overlapOffset;
        float targetRight = -halfScreenWidth + overlapOffset;

        leftWhitePanel.DOAnchorPosX(targetLeft, transitionDuration).SetEase(Ease.InOutSine);

        rightWhitePanel.DOAnchorPosX(targetRight, transitionDuration).SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                StartCoroutine(LoadSceneAsync());
            });
    }

    /// <summary>
    /// Coroutine xử lý tải Scene ngầm và cập nhật thanh tiến độ
    /// </summary>
    private IEnumerator LoadSceneAsync()
    {
        if (loadingGroup != null) loadingGroup.SetActive(true);

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(nextSceneIndex);

        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";
            }

            if (asyncOperation.progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
            }

            yield return null; 
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void RestartGame()
    {
        confirmDialog.Show(
            "Do you want to restart the game?",
            Restart,
            () => { }
        );
    }

    private void Restart()
    {
        if (isTransitioning) return;

        var saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");

        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[Main Menu] 🚨 Đã xóa dữ liệu file save cũ thành công để chuẩn bị chơi mới!");
            }
            else
            {
                Debug.Log("[Main Menu] Không tìm thấy file save cũ, tiến hành tạo mới hoàn toàn.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Main Menu] Lỗi khi xóa file save: {e.Message}");
        }
    }
 
}