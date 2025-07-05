using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log($"Game State: End");

        //context.UIManager.ShowUI(GameStateType.End, UINames.End);

    }

    public void Exit(GameStateContext context)
    {
        //context.UIManager.HideUI(GameStateType.End);

    }

    public void Tick(GameStateContext context)
    {

    }
}
