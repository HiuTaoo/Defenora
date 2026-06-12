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

        WalletManager.OnCoinChanged += HandleCoinChange;
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1;
        InitializeStateMachine();

        SpawnManager.OnAllSpawnPointsDestroyed += HandleAllSpawnPointsDestroyed;

        yield return StartCoroutine(StartGameCoroutine());
    }

    private void InitializeStateMachine()
    {
        StateMachine = new GameStateMachine();

        gameContext = new GameStateContext(StateMachine)
        {
            UIManager = UIManager.Instance,         
            CameraManager = CameraManager.Instance,
            AudioManager = AudioManager.Instance,
            InputManager = new InputManager()      
        };

        StateMachine.SetContext(gameContext);

        StateMachine.RegisterState(GameStateType.Playing, new PlayingState());
        StateMachine.RegisterState(GameStateType.Paused, new PausedState());
        StateMachine.RegisterState(GameStateType.Editor, new EditorState());
        StateMachine.RegisterState(GameStateType.Win, new WinState());
        StateMachine.RegisterState(GameStateType.GameOver, new GameOverState());
    }

    private void Update()
    {
        StateMachine.Tick();
    }

    #region Method
    public void QuitGame()
    {
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

    #region Game Flow Management

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
            Debug.Log("[GameManager] Không có file save (hoặc vừa bấm RESTART)! Bắt đầu tạo thế giới...");

            var isWorldGenerationValid = false;
            var generationRetries = 0;
            const int maxGenerationRetries = 5;

            while (!isWorldGenerationValid && generationRetries < maxGenerationRetries)
            {
                if (GraphNode.Instance != null) GraphNode.Instance.ResetAllWalkableNodesOnly();

                if (ObjectSpawner.Instance != null)
                {
                    ApplyStartGameSettings();
                    yield return null;

                    if (SpawnManager.Instance != null && PlayerController.Instance != null)
                    {
                        var pPos = PlayerController.Instance.transform.position;
                        var pLayer = PlayerController.Instance.GetCurrentLayerIndex();

                        Debug.Log(
                            $"[GameManager] [Lần thử {generationRetries + 1}] Đang quét tìm đường đặt cổng quái liên thông...");

                        var spawnSuccess = SpawnManager.Instance.GenerateSpawnPointsWithSafeZone(2, pPos, pLayer);

                        if (spawnSuccess)
                        {
                            Debug.Log(
                                "[GameManager] 🟢 Tạo bản đồ và các cổng quái thành công! Bản đồ hợp lệ hoàn toàn.");
                            isWorldGenerationValid = true;
                        }
                        else
                        {
                            generationRetries++;
                            Debug.LogWarning(
                                $"[GameManager] ⚠️ Thất bại khi tìm vị trí đặt cổng thông suốt tới Player. Tiến hành dọn dẹp và RE-BAKE lại map lần {generationRetries}...");

                            SaveLoadSystem.Instance.ClearCurrentSceneObjects();
                        }
                    }
                    else
                    {
                        Debug.LogError("[GameManager] ❌ Thiếu cấu phần hệ thống quan trọng để check sinh cổng!");
                        yield break;
                    }
                }
            }

            if (!isWorldGenerationValid)
            {
                Debug.LogError(
                    "[GameManager] 🛑 LỖI CHÍ MẠNG: Đã thử tái tạo map 5 lần nhưng không thể đặt cổng quái hợp lệ liên thông tới Player! Chặn không cho StartGame.");
                SaveLoadSystem.Instance.ClearCurrentSceneObjects();
                QuitGame();
                yield break; 
            }
        }
        ChangeToPlayingState();

        yield return new WaitForEndOfFrame();
        SaveLoadSystem.Instance.SaveGame();
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        StartCoroutine(StartGameCoroutine());
        gameContext.StateMachine.ChangeState(GameStateType.Playing);
    }

    public void RestartGame()
    {
        if (SaveLoadSystem.Instance == null) return;

        Debug.Log("[GameManager] 🚨 Yêu cầu khởi động lại! Bắt đầu thiết lập lại State Machine khẩn cấp...");

        Time.timeScale = 1f;

        ChangeToPlayingState();

        if (SaveLoadSystem.Instance.HasSaveData()) SaveLoadSystem.Instance.DeleteSaveData();

        SaveLoadSystem.Instance.ClearCurrentSceneObjects();

        StartGame();
    }

    private void ApplyStartGameSettings()
    {
        if (GraphNode.Instance != null)
        {
            GraphNode.Instance.ResetAllWalkableNodesOnly();
        }
        else
            Debug.LogError("[GameManager] Không tìm thấy GraphNode.Instance để kiến tạo tài nguyên!");

        if (ObjectSpawner.Instance != null) ObjectSpawner.Instance.SpawnObjectsOnAllLayers();

        WalletManager.Instance.SetCoinsOnLoad(20);
        ShopManager.Instance.GenerateDailyItems();
        if (TimeOfDaySystem.Instance != null)
        {
            TimeOfDaySystem.Instance.SetCurrentDay(1);
            TimeOfDaySystem.Instance.SetCurrentTime(5.9f);
        }

        var node = GraphNode.Instance.GetBestWalkableNodeArea();
        if (GraphNode.Instance.GetNodeWorldData(node, out var spawnPos, out var layerIndex))
        {
            var player = PlayerController.Instance;

            player.transform.position = spawnPos;
            if (player.rb != null) player.rb.position = spawnPos;

            if (player.characterMovement != null) player.characterMovement.CurrentLayer = layerIndex;
            if (player.floorAgent != null) player.floorAgent.MoveToFloor(layerIndex);
        }
    }

    #endregion

    #region Win-Lose

    private void HandleAllSpawnPointsDestroyed()
    {
        if (StateMachine.CurrentStateType != GameStateType.Playing) return;

        Debug.Log(
            "[GameManager] 🏆 ĐIỀU KIỆN THẮNG ĐÃ ĐẠT! Toàn bộ cổng quái vật trên bản đồ đã bị san phẳng hoàn toàn!");
        TriggerGameWin();
    }

    public void HandleCoinChange(int currentCoins)
    {
        if (StateMachine.CurrentStateType != GameStateType.Playing) return;

        if (currentCoins < 0)
        {
            Debug.LogWarning("[GameManager] 🚨 Tài khoản xu của người chơi bị âm! Kích hoạt kết thúc game (THUA)...");
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        StateMachine.ChangeState(GameStateType.GameOver);
        if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame();

        Debug.Log("[GameManager] 💀 GAME OVER! Bạn đã thua cuộc.");
    }

    public void TriggerGameWin()
    {
        Time.timeScale = 0;
        StateMachine.ChangeState(GameStateType.Win);
        if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame();

        Debug.Log("[GameManager] 🏆 VICTORY! Bạn đã hoàn thành màn chơi.");
    }

    #endregion

    #endregion

    private void OnDestroy()
    {
        WalletManager.OnCoinChanged -= HandleCoinChange;

        SpawnManager.OnAllSpawnPointsDestroyed -= HandleAllSpawnPointsDestroyed;
    }
}