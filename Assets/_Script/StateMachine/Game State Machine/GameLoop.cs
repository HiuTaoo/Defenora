using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    public static GameLoop Instance;
    public GameStateMachine StateMachine { get; private set; }

    [Header("GUI For PlayingState")]
    [SerializeField] private GameObject playingGUI;

    [Header("GUI For Pause State")]
    [SerializeField] private GameObject pausedGUI;

    [Header("GUI For Editor State")]
    [SerializeField] private GameObject editorGUI;
    [SerializeField] private GameObject selectUnitGUI;

    [Header("GUI For Main Menu State")]
    [SerializeField] private GameObject mainMenuGUI;

    [Header("GUI For End State")]
    [SerializeField] private GameObject endGUI;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip editorMusic;
    [SerializeField] private AudioClip pauseSound;


    // Managers
    private UIManager uiManager;
    private CameraManager cameraManager;
    private AudioManager audioManager;
    private InputManager inputManager;

    public GameStateContext gameContext {  get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeManagers();
        InitializeStateMachine();
        InitializeEvent();

    }
    #region Initialize
    private void InitializeManagers()
    {
        // Initialize managers
        uiManager = new UIManager();
        cameraManager = new CameraManager(mainCamera);
        audioManager = new AudioManager(musicSource, sfxSource);
        inputManager = new InputManager();

        // Set up UI Manager
        uiManager.RegisterUI(GameStateType.Playing, UINames.MainMenu ,playingGUI, new UIConfig { FadeIn = true });
        uiManager.RegisterUI(GameStateType.Paused, UINames.PauseMenu ,pausedGUI, new UIConfig { FadeIn = true });
        uiManager.RegisterUI(GameStateType.Editor, UINames.EditorMenu, editorGUI, new UIConfig { Scale = Vector3.one * 0.9f });
        uiManager.RegisterUI(GameStateType.Editor, UINames.SelectUnitGUI, selectUnitGUI, new UIConfig { FadeIn = true });
        //uiManager.RegisterUI(GameStateType.MainMenu, mainMenuGUI);
        //uiManager.RegisterUI(GameStateType.End, endGUI);

        // Set up Camera Manager
        cameraManager.SetPlayerTransform(playerTransform);

        cameraManager.RegisterCameraConfig(GameStateType.Playing, new CameraConfig
        {
            FollowPlayer = true,
            SmoothTransition = true,
            OrthographicSize = 5,
            TransitionDuration = 1f
        });

        cameraManager.RegisterCameraConfig(GameStateType.Editor, new CameraConfig
        {
            FollowPlayer = false,
            SmoothTransition = true,
            TransitionDuration = 1.5f
        });

        cameraManager.RegisterCameraConfig(GameStateType.Paused, new CameraConfig
        {
            FollowPlayer = true, // Keep current camera position when paused
            SmoothTransition = false
        });

        // Set up Audio Manager
        /*if (gameplayMusic) audioManager.RegisterAudio("gameplay_music", gameplayMusic);
        if (editorMusic) audioManager.RegisterAudio("editor_music", editorMusic);
        if (pauseSound) audioManager.RegisterAudio("pause_sound", pauseSound);*/

        // Create context
        gameContext = new GameStateContext(StateMachine)
        {
            UIManager = uiManager,
            CameraManager = cameraManager,
            AudioManager = audioManager,
            InputManager = inputManager
        };

    }

    private void InitializeStateMachine()
    {
        StateMachine = new GameStateMachine();
        StateMachine.SetContext(gameContext);

        // Register states - không cần truyền dependencies nữa!
        StateMachine.RegisterState(GameStateType.Playing, new PlayingState());
        StateMachine.RegisterState(GameStateType.Paused, new PausedState());
        StateMachine.RegisterState(GameStateType.Editor, new EditorState());
    }

    private void InitializeEvent()
    {
        SelectUnitSystem.Instance.OnSelectUnit += HandleSelectUnitUI;

    }
    #endregion
                
    private void Start()
    {
        StateMachine.ChangeState(GameStateType.Playing);
    }

    private void Update()
    {
        StateMachine.Tick();

    }

    private void OnValidate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    #region Event
    private void HandleSelectUnitUI(bool isShowing, bool isAllUI)
    {
        uiManager.stateUIs[GameStateType.Editor].TryGetValue(UINames.SelectUnitGUI, out var selectUI);
        uiManager.stateUIs[GameStateType.Editor].TryGetValue(UINames.EditorMenu, out var editorUI);
        if (isShowing)
        {
            selectUI.SetActive(true);
            editorUI.SetActive(false);
        }
        else
        {
            selectUI.SetActive(false);
            editorUI.SetActive(true);
        }
        if(isAllUI)
        {
            selectUI.SetActive(false);
            editorUI.SetActive(false);
        }
        SelectUnitSystem.Instance.OnLerpToSelectedUnit += LerpToPosition;
    }

    public void LerpToPosition(Vector3 targetPosition)
    {
        float duration = 1f;

        if (cameraManager.virtualCamera == null)
        {
            Debug.LogError("CameraManager: virtualCamera is null");
            return;
        }

        if (cameraManager.virtualCamera.Follow != null)
        {
            Debug.Log("CameraManager: Disabling Follow to move manually");
            cameraManager.virtualCamera.Follow = null;
            cameraManager.isFollowingPlayer = false;
        }

        if (cameraManager.currentTransition != null)
        {
            StopCoroutine(cameraManager.currentTransition);
        }

        cameraManager.currentTransition = StartCoroutine(LerpPositionCoroutine(targetPosition, duration));
    }

    private IEnumerator LerpPositionCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = cameraManager.virtualCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cameraManager.mainCamera.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        cameraManager.mainCamera.transform.position = targetPosition;

        cameraManager.currentTransition = null;
    }

    #endregion
}