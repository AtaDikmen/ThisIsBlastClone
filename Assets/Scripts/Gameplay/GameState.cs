using UnityEngine;

namespace Gameplay
{
    public enum GameState
    {
        MainMenu,
        Initializing,
        WaitingForInput,
        ExecutingAction,
        LevelWon,
        LevelFailed
    }

    public interface IGameplayStateMachine
    {
        GameState CurrentState    { get; }
        bool      CanProcessInput { get; }
        void      ChangeState(GameState newState);
    }

    public class GameplayStateMachine : IGameplayStateMachine
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public bool CanProcessInput => CurrentState == GameState.WaitingForInput;

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[StateMachine] State Changed -> {newState}");
        }
    }
}
