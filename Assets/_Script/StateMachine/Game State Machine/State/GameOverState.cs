using UnityEngine;

public class GameOverState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log("You Lose");
        context.UIManager.ShowStateUI(GameStateType.GameOver);
    }

    public void Exit(GameStateContext context)
    {
        

    }

    public void Tick(GameStateContext context)
    {

    }

}
