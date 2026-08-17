using Block;
using Level;
using Services;
using Shooter;
using UI;
using UnityEngine;
using VContainer;

namespace Gameplay
{
    public class GameplayController : MonoBehaviour
    {
        private LevelGenerator        _levelGenerator;
        private LevelProviderService  _levelProviderService;
        private ShooterQueue          _shooterQueue;
        private ShooterSlotManager    _slotManager;
        private GridManager           _gridManager;
        private GameplayHUDController _hudController;
        private IGameplayStateMachine _stateMachine;

        [Inject]
        public void Construct(
            LevelGenerator        levelGenerator,
            LevelProviderService  levelProviderService,
            ShooterQueue          shooterQueue,
            ShooterSlotManager    slotManager,
            GridManager           gridManager,
            GameplayHUDController hudController,
            IGameplayStateMachine stateMachine)
        {
            _levelGenerator       = levelGenerator;
            _levelProviderService = levelProviderService;
            _shooterQueue         = shooterQueue;
            _slotManager          = slotManager;
            _gridManager          = gridManager;
            _hudController        = hudController;
            _stateMachine         = stateMachine;
        }

        public void InitializeLevel()
        {
            _stateMachine.ChangeState(GameState.Initializing);

            var currentLevel = _levelProviderService.GetCurrentLevelData();
            if(currentLevel == null)
            {
                Debug.LogError("[GameplayController] LevelData bulunamadı!");
                return;
            }

            var gridColumns = _levelGenerator.GenerateGrid(currentLevel);

            if(_gridManager != null)
                _gridManager.RegisterColumns(gridColumns);
            else
                Debug.LogError("[GameplayController] GridManager bulunamadı! Ateş etme çalışmaz.");

            int totalBlocks = _levelGenerator.GetActiveBlockCount();
            _hudController.SetupHUD(totalBlocks);

            if(_slotManager != null)
                _slotManager.InitializeSlots(currentLevel.SlotCount);

            if(_shooterQueue != null)
                _shooterQueue.InitializeQueue(currentLevel, _levelGenerator.ApplyBlockColor);

            Debug.Log($"[GameplayController] Level {currentLevel.name} kuruldu. Otomatik ateşlemeye hazır.");
        }

        public void ClearCurrentLevel()
        {
            _levelGenerator.ClearLevel();
            if(_shooterQueue != null) _shooterQueue.ClearQueue();
            if(_slotManager != null) _slotManager.ClearSlots();
        }
    }
}
