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
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField] private Image transitionLoadingBar;
        [SerializeField] private float fakeLoadingDuration = 1.5f;

        private MainMenuUIController  _mainMenuUI;
        private GameplayHUDController _gameplayHUD;
        private GameplayController    _gameplayController;
        private IGameplayStateMachine _stateMachine;

        [Inject]
        public void Construct(MainMenuUIController mainMenuUI, GameplayHUDController gameplayHUD, GameplayController gameplayController, IGameplayStateMachine stateMachine)
        {
            _mainMenuUI         = mainMenuUI;
            _gameplayHUD        = gameplayHUD;
            _gameplayController = gameplayController;
            _stateMachine       = stateMachine;
        }

        private void Start()
        {
            _mainMenuUI.OnPlayClicked += HandlePlayClicked;
            _stateMachine.ChangeState(GameState.MainMenu);

            if(_gameplayHUD != null) _gameplayHUD.gameObject.SetActive(false);
            if(transitionCanvasGroup != null) SetCanvasGroupState(transitionCanvasGroup, false);

            _mainMenuUI.ShowMenu();
        }

        private void OnDestroy()
        {
            if(_mainMenuUI != null)
                _mainMenuUI.OnPlayClicked -= HandlePlayClicked;
        }

        private void HandlePlayClicked()
        {
            StartLevelSequenceAsync().Forget();
        }

        private async UniTaskVoid StartLevelSequenceAsync()
        {
            if(transitionLoadingBar != null) transitionLoadingBar.fillAmount = 0f;

            transitionCanvasGroup.gameObject.SetActive(true);
            transitionCanvasGroup.interactable   = true;
            transitionCanvasGroup.blocksRaycasts = true;

            if(Mathf.Abs(transitionCanvasGroup.alpha - 1f) > 0.01f)
                await Tween.Alpha(transitionCanvasGroup, endValue: 1f, duration: 0.25f).ToYieldInstruction();

            await _mainMenuUI.HideMenuAsync();

            if(transitionLoadingBar != null)
            {
                await Tween.UIFillAmount(transitionLoadingBar, endValue: 1f, duration: fakeLoadingDuration, ease: Ease.InOutQuad)
                           .ToYieldInstruction();
            }

            _gameplayController.InitializeLevel();

            if(_gameplayHUD != null) _gameplayHUD.gameObject.SetActive(true);

            transitionCanvasGroup.blocksRaycasts = false;
            transitionCanvasGroup.interactable   = false;

            if(Mathf.Abs(transitionCanvasGroup.alpha - 0f) > 0.01f)
                await Tween.Alpha(transitionCanvasGroup, endValue: 0f, duration: 0.3f).ToYieldInstruction();

            transitionCanvasGroup.gameObject.SetActive(false);
            _stateMachine.ChangeState(GameState.WaitingForInput);
        }

        private void SetCanvasGroupState(CanvasGroup group, bool active)
        {
            if(group == null) return;
            group.alpha          = active ? 1f : 0f;
            group.interactable   = active;
            group.blocksRaycasts = active;
        }
    }
}
