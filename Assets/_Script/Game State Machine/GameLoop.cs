using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    public static GameLoop Instance;
    public GameStateMachine StateMachine { get; private set; }

    [Header("GUI For GameState")]
    [SerializeField] private GameObject playingGUI;
    [SerializeField] private GameObject pausedGUI;
    [SerializeField] private GameObject editorGUI;
    [SerializeField] private GameObject mainMenuGUI;
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
    }

    private void InitializeManagers()
    {
        // Initialize managers
        uiManager = new UIManager();
        cameraManager = new CameraManager(mainCamera);
        audioManager = new AudioManager(musicSource, sfxSource);
        inputManager = new InputManager();

        // Set up UI Manager
        uiManager.RegisterUI(GameStateType.Playing, playingGUI, new UIConfig { FadeIn = true });
        uiManager.RegisterUI(GameStateType.Paused, pausedGUI, new UIConfig { FadeIn = true });
        uiManager.RegisterUI(GameStateType.Editor, editorGUI, new UIConfig { Scale = Vector3.one * 0.9f });
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

    private void Start()
    {
        StateMachine.ChangeState(GameStateType.Playing);
    }

    private void Update()
    {
        StateMachine.Tick();

        // Update managers that need per-frame updates
        cameraManager.Update();
    }

    private void OnValidate()
    {
        // Validation trong editor
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
}