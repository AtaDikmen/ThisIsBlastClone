using Services;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UI
{
    public class GameplayUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
    
        private ISaveService  _saveService;
        private VisualElement _winPopup;
        private VisualElement _failPopup;
        private Button        _nextButton;
        private Button        _retryButton;

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            _winPopup  = root.Q<VisualElement>("WinPopup");
            _failPopup = root.Q<VisualElement>("FailPopup");
        
            _nextButton  = root.Q<Button>("BtnNext");
            _retryButton = root.Q<Button>("BtnRetry");

            if (_nextButton != null) _nextButton.clicked   += OnNextClicked;
            if (_retryButton != null) _retryButton.clicked += OnRetryClicked;

            HideAllPopups();
        }

        public void ShowWinUI()
        {
            if (_winPopup != null) _winPopup.style.display = DisplayStyle.Flex;
        }

        public void ShowFailUI()
        {
            if (_failPopup != null) _failPopup.style.display = DisplayStyle.Flex;
        }

        private void HideAllPopups()
        {
            if (_winPopup != null) _winPopup.style.display   = DisplayStyle.None;
            if (_failPopup != null) _failPopup.style.display = DisplayStyle.None;
        }

        private void OnNextClicked()
        {
            int nextLevel = _saveService.GetCurrentLevel() + 1;
            _saveService.SaveCurrentLevel(nextLevel);
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        private void OnRetryClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
