using System;
using Audio;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Main Menu Overlay")]
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private TMP_Text mainMenuLevelText;
        [SerializeField] private Button   playButton;
        [SerializeField] private Button   settingsButton;

        [Header("Gameplay HUD")]
        [SerializeField] private CanvasGroup gameplayHudCanvasGroup;
        [SerializeField] private TMP_Text hudLevelText;
        [SerializeField] private Image    hudProgressBar;
        [SerializeField] private Button   homeButton;

        [Header("Win Popup")]
        [SerializeField] private CanvasGroup winPopupCanvasGroup;
        [SerializeField] private Transform winPopupBox;
        [SerializeField] private Button    nextLevelButton;

        [Header("Fail Popup")]
        [SerializeField] private CanvasGroup failPopupCanvasGroup;
        [SerializeField] private Transform failPopupBox;
        [SerializeField] private Button    retryButton;
        [SerializeField] private Button    failMainMenuButton;

        [Header("Settings Popup")]
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private Button      closeSettingsButton;
        [SerializeField] private Toggle      sfxToggle;
        [SerializeField] private Toggle      musicToggle;

        private ISaveService  _saveService;
        private IAudioService _audioService;

        private int _totalBlocksInLevel;

        public event Action OnPlayClicked;
        public event Action OnMainMenuClicked;
        public event Action OnRetryClicked;
        public event Action OnNextLevelClicked;
        public event Action OnHomeClicked;

        [Inject]
        public void Construct(ISaveService saveService, IAudioService audioService)
        {
            _saveService  = saveService;
            _audioService = audioService;
        }

        private void Awake()
        {
            BindButtonEvents();
            ResetAllPanels();
        }

        private void Start()
        {
            RefreshLevelData();
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            UnbindButtonEvents();
        }

        private void BindButtonEvents()
        {
            if(playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
            if(nextLevelButton != null) nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
            if(retryButton != null) retryButton.onClick.AddListener(HandleRetryClicked);
            if(failMainMenuButton != null) failMainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            if(homeButton != null) homeButton.onClick.AddListener(HandleHomeClicked);
            if(settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if(closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

            if(sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(isOn => _audioService?.SetSFXState(!isOn));

            if(musicToggle != null)
                musicToggle.onValueChanged.AddListener(isOn => _audioService?.SetMusicState(!isOn));
        }

        private void UnbindButtonEvents()
        {
            if(playButton != null) playButton.onClick.RemoveAllListeners();
            if(nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
            if(retryButton != null) retryButton.onClick.RemoveAllListeners();
            if(failMainMenuButton != null) failMainMenuButton.onClick.RemoveAllListeners();
            if(homeButton != null) homeButton.onClick.RemoveAllListeners();
            if(settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if(closeSettingsButton != null) closeSettingsButton.onClick.RemoveAllListeners();
        }

        public void ResetAllPanels()
        {
            SetCanvasGroupState(mainMenuCanvasGroup, false);
            SetCanvasGroupState(gameplayHudCanvasGroup, false);
            SetCanvasGroupState(winPopupCanvasGroup, false);
            SetCanvasGroupState(failPopupCanvasGroup, false);

            if(settingsPopup != null) settingsPopup.SetActive(false);
        }

        public void RefreshLevelData()
        {
            if(_saveService == null) return;

            int    levelIndex = _saveService.GetCurrentLevelIndex();
            string levelStr   = $"LEVEL {levelIndex + 1}";

            if(mainMenuLevelText != null) mainMenuLevelText.text = levelStr;
            if(hudLevelText != null) hudLevelText.text           = levelStr;
        }

        public void ShowMainMenu()
        {
            ResetAllPanels();
            RefreshLevelData();
            SetCanvasGroupState(mainMenuCanvasGroup, true);
        }

        public async UniTask HideMainMenuAsync()
        {
            if(mainMenuCanvasGroup != null)
                await Tween.Alpha(mainMenuCanvasGroup, endValue: 0f, duration: 0.2f).ToYieldInstruction();

            SetCanvasGroupState(mainMenuCanvasGroup, false);
        }

        public void ShowGameplayHUD(int totalBlocks)
        {
            _totalBlocksInLevel = totalBlocks;

            if(hudProgressBar != null)
                hudProgressBar.fillAmount = 0f;

            SetCanvasGroupState(gameplayHudCanvasGroup, true);
            Debug.Log($"[UIManager] HUD Kuruldu. Toplam Blok: {_totalBlocksInLevel}");
        }

        public void UpdateBlockProgress(int remainingBlocks)
        {
            if(_totalBlocksInLevel <= 0 || hudProgressBar == null) return;

            int clampedRemaining = Mathf.Clamp(remainingBlocks, 0, _totalBlocksInLevel);
            int destroyedBlocks  = _totalBlocksInLevel - clampedRemaining;

            float targetFill = (float)destroyedBlocks / _totalBlocksInLevel;

            if(Mathf.Approximately(hudProgressBar.fillAmount, targetFill)) return;

            Tween.StopAll(hudProgressBar);
            Tween.UIFillAmount(hudProgressBar, endValue: targetFill, duration: 0.2f, ease: Ease.OutQuad);
        }

        public void ShowWinUI()
        {
            SetCanvasGroupState(winPopupCanvasGroup, true);
            if(winPopupBox != null)
            {
                winPopupBox.localScale = Vector3.zero;
                Sequence.Create()
                        .Group(Tween.Alpha(winPopupCanvasGroup, startValue: 0f, endValue: 1f, duration: 0.2f))
                        .Group(Tween.Scale(winPopupBox, endValue: Vector3.one, duration: 0.3f, ease: Ease.OutBack));
            }
        }

        public void ShowFailUI()
        {
            SetCanvasGroupState(failPopupCanvasGroup, true);
            if(failPopupBox != null)
            {
                failPopupBox.localScale = Vector3.zero;
                Sequence.Create()
                        .Group(Tween.Alpha(failPopupCanvasGroup, startValue: 0f, endValue: 1f, duration: 0.2f))
                        .Group(Tween.Scale(failPopupBox, endValue: Vector3.one, duration: 0.3f, ease: Ease.OutBack));
            }
        }

        private void HandlePlayClicked()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            OnPlayClicked?.Invoke();
        }

        private void HandleNextLevelClicked()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            OnNextLevelClicked?.Invoke();
        }

        private void HandleRetryClicked()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            OnRetryClicked?.Invoke();
        }

        private void HandleMainMenuClicked()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            OnMainMenuClicked?.Invoke();
        }

        private void HandleHomeClicked()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            OnHomeClicked?.Invoke();
        }

        private void OpenSettings()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            if(settingsPopup != null) settingsPopup.SetActive(true);
            if(settingsCanvasGroup != null)
            {
                settingsCanvasGroup.alpha = 0f;
                Tween.Alpha(settingsCanvasGroup, endValue: 1f, duration: 0.2f);
            }
        }

        private void CloseSettings()
        {
            _audioService?.PlaySFX(SoundType.ButtonClick);
            if(settingsCanvasGroup != null)
            {
                Tween.Alpha(settingsCanvasGroup, endValue: 0f, duration: 0.15f)
                     .OnComplete(() => settingsPopup.SetActive(false));
            }
            else if(settingsPopup != null)
            {
                settingsPopup.SetActive(false);
            }
        }

        private void SetCanvasGroupState(CanvasGroup group, bool active)
        {
            if(group == null) return;

            if(active && !group.gameObject.activeSelf)
                group.gameObject.SetActive(true);

            group.alpha          = active ? 1f : 0f;
            group.interactable   = active;
            group.blocksRaycasts = active;

            if(!active && group.gameObject.activeSelf)
                group.gameObject.SetActive(false);
        }
    }
}
