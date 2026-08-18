using Audio;
using Cysharp.Threading.Tasks;
using Gameplay;
using PrimeTween;
using UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core
{
    public class GameFlowController : MonoBehaviour
    {
        [Header("Transition / Loading Overlay")]
        [SerializeField] private GameObject transitionOverlay;
        [SerializeField] private Image transitionLoadingBar;
        [SerializeField] private float loadingDuration = 1.0f;

        private UIManager             _uiManager;
        private GameplayController    _gameplayController;
        private IGameplayStateMachine _stateMachine;
        private IAudioService         _audioService;
        private EnvironmentManager    _environmentManager;

        [Inject]
        public void Construct(
            UIManager             uiManager,
            GameplayController    gameplayController,
            IGameplayStateMachine stateMachine,
            IAudioService         audioService,
            EnvironmentManager    environmentManager)
        {
            _uiManager          = uiManager;
            _gameplayController = gameplayController;
            _stateMachine       = stateMachine;
            _audioService       = audioService;
            _environmentManager = environmentManager;
        }

        private void Start()
        {
            SubscribeEvents();

            _stateMachine.ChangeState(GameState.MainMenu);
            _audioService?.PlayMusic(SoundType.MainMenuMusic);

            SetLoadingState(false);
            ShowInitialMainMenu();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if(_uiManager != null)
            {
                _uiManager.OnPlayClicked     += HandlePlayClicked;
                _uiManager.OnMainMenuClicked += HandleMainMenuClicked;
            }
        }

        private void UnsubscribeEvents()
        {
            if(_uiManager != null)
            {
                _uiManager.OnPlayClicked     -= HandlePlayClicked;
                _uiManager.OnMainMenuClicked -= HandleMainMenuClicked;
            }
        }

        private void ShowInitialMainMenu()
        {
            _environmentManager?.ActivateMainMenuEnvironment();
            _uiManager.ShowMainMenu();
        }

        private void HandlePlayClicked()
        {
            StartLevelSequenceAsync().Forget();
        }

        private void HandleMainMenuClicked()
        {
            ReturnToMainMenuSequenceAsync().Forget();
        }

        private async UniTaskVoid StartLevelSequenceAsync()
        {
            try
            {
                _audioService?.CrossFadeMusicAsync(SoundType.GameplayMusic, 0.5f).Forget();

                SetLoadingState(true);

                await PlayLoadingAnimationAsync();

                _uiManager.ResetAllPanels();
                _environmentManager?.ActivateGameplayEnvironment();
                _gameplayController.InitializeLevel();
            }
            finally
            {
                SetLoadingState(false);
                _stateMachine.ChangeState(GameState.WaitingForInput);
            }
        }

        private async UniTaskVoid ReturnToMainMenuSequenceAsync()
        {
            try
            {
                _audioService?.CrossFadeMusicAsync(SoundType.MainMenuMusic, 0.5f).Forget();

                SetLoadingState(true);

                _gameplayController.ClearCurrentLevel();

                await PlayLoadingAnimationAsync();

                _environmentManager?.ActivateMainMenuEnvironment();
                _uiManager.ShowMainMenu();
            }
            finally
            {
                SetLoadingState(false);
                _stateMachine.ChangeState(GameState.MainMenu);
            }
        }

        private async UniTask PlayLoadingAnimationAsync()
        {
            if(transitionLoadingBar != null)
            {
                transitionLoadingBar.fillAmount = 0f;
                await Tween.UIFillAmount(transitionLoadingBar, endValue: 1f, duration: loadingDuration, ease: Ease.InOutQuad)
                           .ToYieldInstruction();
            }
            else
            {
                await UniTask.Delay((int)(loadingDuration * 1000));
            }
        }

        private void SetLoadingState(bool active)
        {
            if(transitionOverlay != null)
            {
                transitionOverlay.SetActive(active);
            }
        }
    }
}
