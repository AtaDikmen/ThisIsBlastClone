using System;
using Audio;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup menuCanvasGroup;

        [Header("Top Bar Display")]
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text lifeText;
        [SerializeField] private Button   settingsButton;

        [Header("Center Play Area")]
        [SerializeField] private TMP_Text levelButtonText;
        [SerializeField] private Button playLevelButton;

        [Header("Settings Popup")]
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private Button      closeSettingsButton;
        [SerializeField] private Toggle      sfxToggle;
        [SerializeField] private Toggle      musicToggle;

        private ISaveService  _saveService;
        private IAudioService _audioService;

        public event Action OnPlayClicked;

        [Inject]
        public void Construct(ISaveService saveService, IAudioService audioService)
        {
            _saveService  = saveService;
            _audioService = audioService;
        }

        private void Awake()
        {
            if(playLevelButton != null) playLevelButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
            if(settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if(closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

            if(sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(isOn => _audioService.SetSFXState(!isOn));

            if(musicToggle != null)
                musicToggle.onValueChanged.AddListener(isOn => _audioService.SetMusicState(!isOn));
        }

        private void OnDestroy()
        {
            if(playLevelButton != null) playLevelButton.onClick.RemoveAllListeners();
            if(settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if(closeSettingsButton != null) closeSettingsButton.onClick.RemoveAllListeners();
        }

        public void RefreshUI()
        {
            if(_saveService == null) return;

            if(coinText != null)
                coinText.text = _saveService.GetCoins().ToString("N0");

            if(lifeText != null)
                lifeText.text = _saveService.GetLives().ToString();

            if(levelButtonText != null)
                levelButtonText.text = $"LEVEL {_saveService.GetCurrentLevel() + 1}";
        }

        public void ShowMenu()
        {
            RefreshUI();
            SetCanvasGroupState(menuCanvasGroup, true);
        }

        public async UniTask HideMenuAsync()
        {
            await Tween.Alpha(menuCanvasGroup, endValue: 0f, duration: 0.3f).ToYieldInstruction();
            SetCanvasGroupState(menuCanvasGroup, false);
        }

        private void OpenSettings()
        {
            if(settingsPopup != null) settingsPopup.SetActive(true);
            if(settingsCanvasGroup != null)
            {
                settingsCanvasGroup.alpha = 0f;
                Tween.Alpha(settingsCanvasGroup, endValue: 1f, duration: 0.2f);
            }
        }

        private void CloseSettings()
        {
            if(settingsCanvasGroup != null)
            {
                Tween.Alpha(settingsCanvasGroup, endValue: 0f, duration: 0.2f)
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
            group.alpha          = active ? 1f : 0f;
            group.interactable   = active;
            group.blocksRaycasts = active;
        }
    }
}
