namespace _Script.StateMachine.Game_State_Machine.State
{
    public class WinState : IGameState
    {
        public void Enter(GameStateContext context)
        {
            context.UIManager.HideAllUIs();
            context.UIManager.ShowStateUI(GameStateType.Win);
            AudioManager.Instance.PlaySFX(SoundNames.SfxLevelWin);
            context.AudioManager.PlayMusic(SoundNames.VictoryTheme);
        }

        public void Exit(GameStateContext context)
        {
        }

        public void Tick(GameStateContext context)
        {
        }
    }
}