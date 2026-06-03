using System.Collections;
using _Script.StateMachine.Game_State_Machine.State;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameStateMachine StateMachine { get; private set; }
    public GameStateContext gameContext { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }

    private IEnumerator Start()
    {
        InitializeStateMachine();

        // Chạy tiến trình khởi chạy game và đợi cho đến khi hoàn thành xong xuôi
        yield return StartCoroutine(StartGameCoroutine());
    }

    private void InitializeStateMachine()
    {
        StateMachine = new GameStateMachine();

        // Nạp các Singleton Manager vào Context
        gameContext = new GameStateContext(StateMachine)
        {
            // Các Manager này giờ là MonoBehaviour tự khởi tạo từ đầu
            UIManager = UIManager.Instance,         
            CameraManager = CameraManager.Instance,
            AudioManager = AudioManager.Instance,   // Nếu bạn cũng đã làm AudioManager thành MonoBehaviour
            InputManager = new InputManager()       // Cái này không cần gắn GameObject thì vẫn để C# thường
        };

        StateMachine.SetContext(gameContext);

        StateMachine.RegisterState(GameStateType.Playing, new PlayingState());
        StateMachine.RegisterState(GameStateType.Paused, new PausedState());
        StateMachine.RegisterState(GameStateType.Editor, new EditorState());
        
        
    }


    private void Update()
    {
        StateMachine.Tick();
    }

    #region Method
    public void QuitGame()
    {
        //Application.Quit();
        SceneManager.LoadScene(0);
    }

    public void ChangeToPlayingState()
    {
        StateMachine.ChangeState(GameStateType.Playing);
    }

    public void ChangeToEditorState()
    {
        StateMachine.ChangeState(GameStateType.Editor);
    }

    #region GUI

    public void OpenSettingInPauseMenu()
    {
        UIManager.Instance.HideUI(GameStateType.Paused, UINames.PauseButton);
        UIManager.Instance.ShowUI(GameStateType.Paused, UINames.PauseMenuSetting);
    }

    public void BackToPauseMenu()
    {
        UIManager.Instance.HideUI(GameStateType.Paused, UINames.PauseMenuSetting);
        UIManager.Instance.ShowUI(GameStateType.Paused, UINames.PauseButton);
    }

    public void ResumeGame()
    {
        gameContext.StateMachine.ChangeState(GameStateType.Playing);
    }

    public void PauseGame()
    {
        gameContext.StateMachine.ChangeState(GameStateType.Paused);
    }

    public void OpenAvailableUnitGUI()
    {
        UIManager.Instance.ShowUI(GameStateType.Editor, UINames.AvailableUnitsGUI);
    }

    public void OpenInventoryGUI()
    {
        UIManager.Instance.HideStateUI(GameStateType.Playing);
        UIManager.Instance.ShowUI(GameStateType.Playing, UINames.Inventory);
    }

    public void OpenShopGUI()
    {
        UIManager.Instance.HideStateUI(GameStateType.Playing);
        UIManager.Instance.ShowUI(GameStateType.Playing, UINames.Shop);
        ShopManager.Instance.InitializeShopUI();
    }

    public void SaveGame()
    {
        SaveLoadSystem.Instance.SaveGame();
    }

    #endregion

    #region Game Flow Management (Khởi Chạy & Chơi Lại)

    private IEnumerator StartGameCoroutine()
    {
        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("[GameManager] Không tìm thấy hệ thống SaveLoadSystem trên Scene!");
            yield break;
        }

        if (SaveLoadSystem.Instance.HasSaveData())
        {
            Debug.Log("[GameManager] Tìm thấy file dữ liệu cũ! Tiến hành nạp màn chơi từ file save...");

            if (SaveLoadSystem.Instance.loadAsync)
                yield return StartCoroutine(SaveLoadSystem.Instance.LoadGameAsync());
            else
                SaveLoadSystem.Instance.LoadGame();
        }
        else
        {
            Debug.Log("[GameManager] File save trống! Bắt đầu tạo dựng thế giới mới tinh...");

            if (ObjectSpawner.Instance != null)
            {
                ApplyStartGameSettings();
                Debug.Log("[GameManager] Đã tạo dựng thành công hệ sinh thái tài nguyên vòng lặp đầu tiên.");
            }
        }

        ChangeToPlayingState();
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    public void RestartGame()
    {
        if (SaveLoadSystem.Instance == null) return;

        Debug.Log("[GameManager] 🚨 Yêu cầu khởi động lại! Tiến hành xóa file dữ liệu save...");

        if (SaveLoadSystem.Instance.HasSaveData()) SaveLoadSystem.Instance.DeleteSaveData();

        StartGame();
    }

    public void ApplyStartGameSettings()
    {
        ObjectSpawner.Instance.SpawnObjectsOnAllLayers();
        if (UnitManager.Instance != null)
            UnitManager.Instance.UpdateGraphNodeWhenStart();
        else
            Debug.LogError("[GameManager] Không tìm thấy ObjectSpawner.Instance để kiến tạo tài nguyên!");
        WalletManager.Instance.SetCoinsOnLoad(10);
        ShopManager.Instance.GenerateDailyItems();
    }

    #endregion

    #endregion
}