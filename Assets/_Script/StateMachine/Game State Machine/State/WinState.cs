namespace _Script.StateMachine.Game_State_Machine.State
{
    public class WinState : IGameState
    {
        public void Enter(GameStateContext context)
        {
            context.UIManager.ShowStateUI(GameStateType.Win);
        }

        public void Exit(GameStateContext context)
        {
        }

        public void Tick(GameStateContext context)
        {
        }
    }
}