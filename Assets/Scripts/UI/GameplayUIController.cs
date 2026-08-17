using System;
using PrimeTween;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class GameplayUIController : MonoBehaviour
    {
        [Header("Main Menu Panel Overlay")]
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private TMP_Text mainMenuLevelText;
        [SerializeField] private Button   playButton;

        [Header("Gameplay HUD")]
        [SerializeField] private TMP_Text hudLevelText;

        [Header("Win Popup")]
        [SerializeField] private CanvasGroup winPopupCanvasGroup;
        [SerializeField] private Transform winPopupBox;
        [SerializeField] private Button    nextLevelButton;

        [Header("Fail Popup")]
        [SerializeField] private CanvasGroup failPopupCanvasGroup;
        [SerializeField] private Transform failPopupBox;
        [SerializeField] private Button    retryButton;

        private ISaveService _saveService;

        public event Action OnPlayClicked;

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        private void Awake()
        {
            if(playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
            if(nextLevelButton != null) nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
            if(retryButton != null) retryButton.onClick.AddListener(HandleRetryClicked);

            ResetAllPanels();
        }

        private void Start()
        {
            UpdateLevelTexts();
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if(playButton != null) playButton.onClick.RemoveAllListeners();
            if(nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
            if(retryButton != null) retryButton.onClick.RemoveAllListeners();
        }

        private void ResetAllPanels()
        {
            SetCanvasGroupState(winPopupCanvasGroup, false);
            SetCanvasGroupState(failPopupCanvasGroup, false);
        }

        private void UpdateLevelTexts()
        {
            int    levelIndex  = _saveService != null ? _saveService.GetCurrentLevel() : 0;
            string levelString = $"LEVEL {levelIndex + 1}";

            if(mainMenuLevelText != null) mainMenuLevelText.text = levelString;
            if(hudLevelText != null) hudLevelText.text           = levelString;
        }

        public void ShowMainMenu()
        {
            SetCanvasGroupState(mainMenuCanvasGroup, true);
        }

        private void HandlePlayClicked()
        {
            Tween.Alpha(mainMenuCanvasGroup, endValue: 0f, duration: 0.3f)
                 .OnComplete(() => SetCanvasGroupState(mainMenuCanvasGroup, false));

            OnPlayClicked?.Invoke();
        }

        public void ShowWinUI()
        {
            SetCanvasGroupState(winPopupCanvasGroup, true);
            winPopupBox.localScale = Vector3.zero;

            Sequence.Create()
                    .Group(Tween.Alpha(winPopupCanvasGroup, startValue: 0f, endValue: 1f, duration: 0.25f))
                    .Group(Tween.Scale(winPopupBox, endValue: Vector3.one, duration: 0.4f, ease: Ease.OutBack));
        }

        public void ShowFailUI()
        {
            SetCanvasGroupState(failPopupCanvasGroup, true);
            failPopupBox.localScale = Vector3.zero;

            Sequence.Create()
                    .Group(Tween.Alpha(failPopupCanvasGroup, startValue: 0f, endValue: 1f, duration: 0.25f))
                    .Group(Tween.Scale(failPopupBox, endValue: Vector3.one, duration: 0.4f, ease: Ease.OutBack));
        }

        private void HandleNextLevelClicked()
        {
            int nextLevel = (_saveService != null ? _saveService.GetCurrentLevel() : 0) + 1;
            _saveService?.SaveCurrentLevel(nextLevel);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleRetryClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
