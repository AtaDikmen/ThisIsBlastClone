using System;
using Audio;
using Block;
using Cysharp.Threading.Tasks;
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
        private UIManager             _uiManager;
        private IGameplayStateMachine _stateMachine;
        private IAudioService         _audioService;

        private bool _isGameEnded = false;

        [Inject]
        public void Construct(
            LevelGenerator        levelGenerator,
            LevelProviderService  levelProviderService,
            ShooterQueue          shooterQueue,
            ShooterSlotManager    slotManager,
            GridManager           gridManager,
            UIManager             uiManager,
            IGameplayStateMachine stateMachine,
            IAudioService         audioService)
        {
            _levelGenerator       = levelGenerator;
            _levelProviderService = levelProviderService;
            _shooterQueue         = shooterQueue;
            _slotManager          = slotManager;
            _gridManager          = gridManager;
            _uiManager            = uiManager;
            _stateMachine         = stateMachine;
            _audioService         = audioService;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void InitializeLevel()
        {
            _isGameEnded = false;
            _stateMachine.ChangeState(GameState.Initializing);

            var currentLevel = _levelProviderService.GetCurrentLevelData();
            if(currentLevel == null)
            {
                Debug.LogError("[GameplayController] LevelData bulunamadı!");
                return;
            }

            UnsubscribeEvents();

            var gridColumns = _levelGenerator.GenerateGrid(currentLevel);

            if(_gridManager != null)
            {
                _gridManager.RegisterColumns(gridColumns);
                _gridManager.OnLevelComplete   += HandleLevelWon;
                _gridManager.OnFrontRowChanged += CheckGameStateAsyncForget;
            }

            int totalBlocks = _gridManager != null ? _gridManager.GetRemainingBlockCount() : 0;
            Debug.Log($"[GameplayController] Başlangıç Toplam Blok Sayısı: {totalBlocks}");

            if(_uiManager != null)
                _uiManager.ShowGameplayHUD(totalBlocks);

            if(_slotManager != null)
                _slotManager.InitializeSlots(currentLevel.SlotCount);

            if(_shooterQueue != null)
            {
                _shooterQueue.InitializeQueue(currentLevel, _levelGenerator.ApplyBlockColor);
                _shooterQueue.OnBlockSelected += OnBlockSelectedHandler;
            }

            _stateMachine.ChangeState(GameState.WaitingForInput);
        }

        private void UnsubscribeEvents()
        {
            if(_gridManager != null)
            {
                _gridManager.OnLevelComplete   -= HandleLevelWon;
                _gridManager.OnFrontRowChanged -= CheckGameStateAsyncForget;
            }

            if(_shooterQueue != null)
            {
                _shooterQueue.OnBlockSelected -= OnBlockSelectedHandler;
            }
        }

        private void OnBlockSelectedHandler(ShooterBlock block)
        {
            CheckGameStateAsyncForget();
        }

        private void CheckGameStateAsyncForget()
        {
            if(_gridManager != null && _uiManager != null)
            {
                int remainingBlocks = _gridManager.GetRemainingBlockCount();
                _uiManager.UpdateBlockProgress(remainingBlocks);
            }

            CheckGameStateValidationAsync().Forget();
        }

        private async UniTaskVoid CheckGameStateValidationAsync()
        {
            if(_isGameEnded) return;

            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));

            if(_isGameEnded) return;

            if(_gridManager != null && _gridManager.IsAllEmpty())
            {
                HandleLevelWon();
                return;
            }

            if(_slotManager != null && _gridManager != null)
            {
                if(_slotManager.IsAllSlotsFull())
                {
                    if(!_slotManager.IsAnyShooterFiring() && !_slotManager.HasAnyValidTargetOnGrid(_gridManager))
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(0.50f));

                        if(_isGameEnded) return;

                        if(_slotManager.IsAllSlotsFull() && !_slotManager.HasAnyValidTargetOnGrid(_gridManager))
                        {
                            HandleLevelFailed();
                        }
                    }
                }
            }
        }

        private void HandleLevelWon()
        {
            if(_isGameEnded) return;
            _isGameEnded = true;

            _stateMachine.ChangeState(GameState.LevelWon);
            _audioService?.PlaySFX(SoundType.Win);

            if(_uiManager != null)
                _uiManager.ShowWinUI();

            Debug.Log("[GameplayController] LEVEL WON!");
        }

        private void HandleLevelFailed()
        {
            if(_isGameEnded) return;
            _isGameEnded = true;

            _stateMachine.ChangeState(GameState.LevelFailed);
            _audioService?.PlaySFX(SoundType.Fail);

            if(_uiManager != null)
                _uiManager.ShowFailUI();

            Debug.Log("[GameplayController] LEVEL FAILED!");
        }

        public void ClearCurrentLevel()
        {
            UnsubscribeEvents();
            _levelGenerator.ClearLevel();
            if(_shooterQueue != null) _shooterQueue.ClearQueue();
            if(_slotManager != null) _slotManager.ClearSlots();
        }
    }
}
