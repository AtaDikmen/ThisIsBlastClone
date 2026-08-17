using Services;
using TMPro;
using UnityEngine;
using VContainer;

namespace UI
{
    public class GameplayHUDController : MonoBehaviour
    {
        [Header("HUD Indicators")]
        [SerializeField] public TMP_Text hudLevelText;
        [SerializeField] public TMP_Text hudLivesText;
        [SerializeField] public TMP_Text remainingBlocksText;

        private ISaveService _saveService;

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void SetupHUD(int totalGridBlocks)
        {
            if(_saveService != null)
            {
                if(hudLevelText != null)
                    hudLevelText.text = $"LEVEL {_saveService.GetCurrentLevel() + 1}";

                if(hudLivesText != null)
                    hudLivesText.text = _saveService.GetLives().ToString();
            }

            UpdateRemainingBlocks(totalGridBlocks);
        }

        public void UpdateRemainingBlocks(int count)
        {
            if(remainingBlocksText != null)
                remainingBlocksText.text = count.ToString();
        }
    }
}
