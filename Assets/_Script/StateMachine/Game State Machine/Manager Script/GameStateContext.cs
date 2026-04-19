using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateContext 
{
    public GameStateMachine StateMachine { get; set; }
    public UIManager UIManager { get; set; }
    public CameraManager CameraManager { get; set; }
    public AudioManager AudioManager { get; set; }
    public InputManager InputManager { get; set; }

    // Constructor để đảm bảo context được tạo đúng cách
    public GameStateContext(GameStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

}
