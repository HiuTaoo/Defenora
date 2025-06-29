using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: MainMenu");

        context.UIManager.ShowUI(GameStateType.MainMenu);

    }

    public void Exit(GameStateContext context)
    {
        context.UIManager.HideUI(GameStateType.End);

    }

    public void Tick(GameStateContext context)
    {

    }

}
