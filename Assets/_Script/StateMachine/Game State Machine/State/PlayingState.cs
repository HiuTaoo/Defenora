using UnityEngine;

namespace _Script.StateMachine.Game_State_Machine.State
{
    public class PlayingState : IGameState
    {
        private enum CurrentTrackState
        {
            None,
            Day,
            Night
        }

        private CurrentTrackState currentPlayingTrack = CurrentTrackState.None;

        public void Enter(GameStateContext context)
        {
            Debug.Log($"Game State: Playing");
            context.UIManager.HideAllUIs();
            context.UIManager.ShowUI(GameStateType.Playing, UINames.GameplayHUD);
            context.CameraManager.ApplyCameraSettings(GameStateType.Playing);

            UpdateBGMBasedOnTime(context);
        }

        public void Exit(GameStateContext context)
        {
            context.UIManager.HideUI(GameStateType.Playing, UINames.MainMenu);
        }

        public void Tick(GameStateContext context)
        {
            if (context.InputManager.GetKeyDown(KeyCode.Escape))
            {
                context.StateMachine.ChangeState(GameStateType.Paused);
            }

            if (context.InputManager.GetKeyUp(KeyCode.F7))
            {
                if (context.InputManager.GetMovementInput() == Vector2.zero)
                    context.StateMachine.ChangeState(GameStateType.Editor);
                else
                    Debug.Log("Không thể mở editor trong trạng thái này!");
            }

            if (context.InputManager.GetKeyUp(KeyCode.B))
            {
                GameManager.Instance.OpenInventoryGUI();
            }

            UpdateBGMBasedOnTime(context);

            HandleGameplayInput(context);
        }

        public void HandleGameplayInput(GameStateContext context)
        {
            Vector2 movement = context.InputManager.GetMovementInput();
        }

        private void UpdateBGMBasedOnTime(GameStateContext context)
        {
            if (TimeOfDaySystem.Instance == null || context.AudioManager == null) return;

            var currentTime = TimeOfDaySystem.Instance.GetCurrentTime();

            var isNightTime = currentTime >= 18f || currentTime < 6f;

            if (isNightTime)
            {
                if (currentPlayingTrack != CurrentTrackState.Night)
                {
                    context.AudioManager.PlayMusic(SoundNames.NightTheme);
                    currentPlayingTrack = CurrentTrackState.Night;
                }
            }
            else
            {
                if (currentPlayingTrack != CurrentTrackState.Day)
                {
                    context.AudioManager.PlayMusic(SoundNames.DayTheme);
                    currentPlayingTrack = CurrentTrackState.Day;
                }
            }
        }
    }
}