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

    [SerializeField] private GameObject audioSettingUI;

    [SerializeField] private AudioSource audioSource;

    [Header("Confirm Dialog")] public ConfirmDialog confirmDialog;

    private bool isTransitioning = false;

    private string saveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

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

        if (audioSettingUI != null)
            audioSettingUI.SetActive(false);

        if (PoolManager.Instance != null)
            PoolManager.Instance.ClearAndRefillPools();
        AudioManager.Instance.PlayMusic(SoundNames.MainMenuTheme);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI() && !isTransitioning)
            {
                if (hideUI != null) hideUI.gameObject.SetActive(false);
                AudioManager.Instance.PauseMusic();
                StartWhiteWipeTransition();
                AudioManager.Instance.PlaySFX(SoundNames.SfxChangeScene);
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

    private IEnumerator LoadSceneAsync()
    {
        if (loadingGroup != null) loadingGroup.SetActive(true);

        var sceneToLoadName = "";

        if (PlayerPrefs.HasKey("RestartingLevelSceneName"))
        {
            sceneToLoadName = PlayerPrefs.GetString("RestartingLevelSceneName");
        }
        else if (File.Exists(saveFilePath))
        {
            try
            {
                var json = File.ReadAllText(saveFilePath);
                var temporaryData = JsonUtility.FromJson<GameSaveData>(json);

                if (temporaryData != null && !temporaryData.isWin && !temporaryData.isGameOver
                    && !string.IsNullOrEmpty(temporaryData.currentLevelSceneName))
                    sceneToLoadName = temporaryData.currentLevelSceneName;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Main Menu] Lỗi đọc nhanh file save để check màn: {e.Message}");
            }
        }

        if (!string.IsNullOrEmpty(sceneToLoadName))
        {
            Debug.Log($"[Main Menu] Tải Scene Level: {sceneToLoadName}");

            var asyncOpSave = SceneManager.LoadSceneAsync(sceneToLoadName);
            asyncOpSave.allowSceneActivation = false;

            while (!asyncOpSave.isDone)
            {
                var progress = Mathf.Clamp01(asyncOpSave.progress / 0.9f);
                if (progressBar != null) progressBar.value = progress;
                if (progressText != null) progressText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";

                if (asyncOpSave.progress >= 0.9f) asyncOpSave.allowSceneActivation = true;
                yield return null;
            }

            yield break;
        }

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
        if (isTransitioning) return;

        confirmDialog.Show(
            "Do you want to RESTART THIS LEVEL only? \n(Press YES to replay current level / Press NO to reset full game progress)",
            RestartCurrentLevel,
            RestartFullGame
        );
    }

    private void RestartCurrentLevel()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("[Main Menu] Không có file save cũ để restart level, tiến hành vào game mặc định.");
            TriggerTransitionToGame();
            return;
        }

        try
        {
            var json = File.ReadAllText(saveFilePath);
            var saveData = JsonUtility.FromJson<GameSaveData>(json);

            if (saveData != null && !string.IsNullOrEmpty(saveData.currentLevelSceneName))
            {
                var preservedSceneName = saveData.currentLevelSceneName;

                PlayerPrefs.SetString("RestartingLevelSceneName", preservedSceneName);
                PlayerPrefs.Save();

                File.Delete(saveFilePath);

                Debug.Log($"[Main Menu] 🔄 Đã xóa save cũ. Gửi tín hiệu tái tạo từ đầu Level: {preservedSceneName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Main Menu] Lỗi khi xử lý trích xuất tên màn để Restart Level: {e.Message}");
        }

        TriggerTransitionToGame();
    }

    private void RestartFullGame()
    {
        try
        {
            PlayerPrefs.DeleteKey("RestartingLevelSceneName");
            PlayerPrefs.Save();

            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("[Main Menu] 🚨 Đã XÓA TOÀN BỘ FILE SAVE. Chơi lại từ đầu game!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Main Menu] Lỗi khi xóa toàn bộ file save: {e.Message}");
        }

        TriggerTransitionToGame();
    }

    private void TriggerTransitionToGame()
    {
        if (hideUI != null) hideUI.gameObject.SetActive(false);
        AudioManager.Instance.PauseMusic();
        StartWhiteWipeTransition();
        AudioManager.Instance.PlaySFX(SoundNames.SfxChangeScene);
    }

    public void OpenAudioSettingUI()
    {
        audioSettingUI.SetActive(true);
    }

    public void CloseAudioSettingUI()
    {
        audioSettingUI.SetActive(false);
    }
}