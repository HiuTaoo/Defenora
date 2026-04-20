using _Script.StateMachine.Game_State_Machine.State;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameStateMachine StateMachine { get; private set; }
    public GameStateContext gameContext { get; private set; }

    // XÓA TẤT CẢ các biến UI, Audio, Camera ở đây! Đã có các Manager khác lo.

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
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

    private void Start()
    {
        InitializeStateMachine();
        StateMachine.ChangeState(GameStateType.Playing);
    }

    private void Update()
    {
        StateMachine.Tick();
    }

    #region Method
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    
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

    public void SaveGame()
    {
        SaveLoadSystem.Instance.SaveGame();
    }

    #endregion
}