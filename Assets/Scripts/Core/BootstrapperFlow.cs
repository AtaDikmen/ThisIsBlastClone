using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Core
{
    public class BootstrapperFlow : MonoBehaviour
    {
        [Header("UI Canvas Groups")]
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private CanvasGroup loadingCanvasGroup;

        [Header("Loading UI")]
        [SerializeField] private Image loadingBarFill;

        [Header("Timing Settings")]
        [SerializeField] private float splashDuration = 1.5f;
        [SerializeField] private float fakeLoadingDuration = 2.0f;

        private ISaveService _saveService;

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        private void Start()
        {
            StartBootSequenceAsync().Forget();
        }

        private async UniTaskVoid StartBootSequenceAsync()
        {
            splashCanvasGroup.alpha   = 1f;
            loadingCanvasGroup.alpha  = 0f;
            loadingBarFill.fillAmount = 0f;

            await UniTask.Delay(TimeSpan.FromSeconds(splashDuration));

            await Sequence.Create()
                          .Group(Tween.Alpha(splashCanvasGroup, endValue: 0f, duration: 0.4f))
                          .Group(Tween.Alpha(loadingCanvasGroup, endValue: 1f, duration: 0.4f));

            await Tween.UIFillAmount(loadingBarFill, endValue: 1f, duration: fakeLoadingDuration, ease: Ease.InOutQuad)
                       .ToYieldInstruction();

            var asyncLoad = SceneManager.LoadSceneAsync("Scene_Gameplay");
            while(asyncLoad is { isDone: false })
            {
                await UniTask.Yield();
            }
        }
    }
}
