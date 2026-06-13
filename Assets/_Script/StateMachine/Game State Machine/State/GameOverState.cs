using UnityEngine;

public class GameOverState : IGameState
{
    public void Enter(GameStateContext context)
    {
        Debug.Log("You Lose");
        context.UIManager.HideAllUIs();
        context.UIManager.ShowStateUI(GameStateType.GameOver);
        AudioManager.Instance.PlaySFX(SoundNames.SfxLevelLose);
        context.AudioManager.PlayMusic(SoundNames.GameOverTheme);
    }

    public void Exit(GameStateContext context)
    {
        

    }

    public void Tick(GameStateContext context)
    {

    }

}
