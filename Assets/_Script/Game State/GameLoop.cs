using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    public static GameLoop Instance;

    public GameStateMachine StateMachine {  get; private set; }

    [Header("GUI For GameState")]
    [SerializeField] private GameObject playingGUI;
    [SerializeField] private GameObject pausedGUI;
    [SerializeField] private GameObject editorGUI;
    [SerializeField] private GameObject mainMenuGUI;
    [SerializeField] private GameObject endGUI;

    private void Awake()
    {
        if (Instance == null) { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else { 
            Destroy(gameObject); 
            return; 
        }

        StateMachine = new GameStateMachine();

        StateMachine.RegisterState(GameStateType.Playing, new PlayingState(StateMachine, playingGUI));
        //StateMachine.RegisterState(GameStateType.MainMenu, new MainMenuState(StateMachine));
        StateMachine.RegisterState(GameStateType.Paused, new PausedState(StateMachine, pausedGUI));
        StateMachine.RegisterState(GameStateType.Editor, new EditorState(StateMachine, editorGUI));
        //StateMachine.RegisterState(GameStateType.End, new EndState(StateMachine));

    }

    private void Start()
    {
        StateMachine.ChangeState(GameStateType.Playing);
    }

    private void Update()
    {
        StateMachine.Tick();
    }
}
