using System;
using System.Threading;
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

        private bool                    _isGameEnded = false;
        private CancellationTokenSource _failCheckCts;

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
            CancelFailCheckToken();
        }

        public void InitializeLevel()
        {
            CancelFailCheckToken();
            UnsubscribeEvents();

            _isGameEnded = false;
            _stateMachine.ChangeState(GameState.Initializing);

            var currentLevel = _levelProviderService.GetCurrentLevelData();
            if(currentLevel == null)
            {
                Debug.LogError("[GameplayController] LevelData bulunamadı!");
                return;
            }

            var gridColumns = _levelGenerator.GenerateGrid(currentLevel);

            if(_gridManager != null)
            {
                _gridManager.RegisterColumns(gridColumns);
                _gridManager.OnLevelComplete   += HandleLevelWon;
                _gridManager.OnFrontRowChanged += CheckGameStateAsyncForget;
            }

            int totalBlocks = _gridManager != null ? _gridManager.GetRemainingBlockCount() : 0;

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
            // 🛑 GUARD 1: Eğer oyun zaten bittiyse gelen inputları kesinlikle işleme!
            if(_isGameEnded) return;

            CheckGameStateAsyncForget();
        }

        private void CheckGameStateAsyncForget()
        {
            if(_isGameEnded) return;

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

            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(0.25f), cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();
            if(isCanceled || _isGameEnded) return;

            if(_gridManager != null && _gridManager.IsAllEmpty())
            {
                HandleLevelWon();
                return;
            }

            if(_slotManager != null && _gridManager != null)
            {
                if(_slotManager.IsAllSlotsFull() && !_slotManager.IsAnyShooterFiring())
                {
                    if(!_slotManager.HasAnyValidTargetOnGrid(_gridManager))
                    {
                        CancelFailCheckToken();
                        _failCheckCts = new CancellationTokenSource();

                        try
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(3f), cancellationToken: _failCheckCts.Token);

                            if(_isGameEnded) return;

                            bool isStillFull    = _slotManager.IsAllSlotsFull();
                            bool isStillFiring  = _slotManager.IsAnyShooterFiring();
                            bool hasValidTarget = _slotManager.HasAnyValidTargetOnGrid(_gridManager);
                            bool isBoardCleared = _gridManager.IsAllEmpty();

                            if(isStillFull && !isStillFiring && !hasValidTarget && !isBoardCleared)
                                HandleLevelFailed();
                        }
                        catch(OperationCanceledException)
                        {
                        }
                    }
                }
            }
        }

        private void HandleLevelWon()
        {
            if(_isGameEnded) return;
            _isGameEnded = true;

            CancelFailCheckToken();
            UnsubscribeEvents();

            _stateMachine.ChangeState(GameState.LevelWon);
            _audioService?.PlaySFX(SoundType.Win);

            ClearRemainingShootersOnWinAsync().Forget();
            if(_uiManager != null)
                _uiManager.ShowWinUI();

            Debug.Log("[GameplayController] LEVEL WON!");
        }

        private async UniTaskVoid ClearRemainingShootersOnWinAsync()
        {
            if(_slotManager != null)
                _slotManager.ClearSlotsWithRunAwayAnimation();

            if(_shooterQueue != null)
                _shooterQueue.ClearQueueWithRunAwayAnimation();

            await UniTask.Yield();
        }

        private void HandleLevelFailed()
        {
            if(_isGameEnded) return;
            _isGameEnded = true;

            CancelFailCheckToken();
            UnsubscribeEvents();

            _stateMachine.ChangeState(GameState.LevelFailed);
            _audioService?.PlaySFX(SoundType.Fail);

            if(_uiManager != null)
                _uiManager.ShowFailUI();

            Debug.Log("[GameplayController] LEVEL FAILED!");
        }

        private void CancelFailCheckToken()
        {
            if(_failCheckCts != null)
            {
                _failCheckCts.Cancel();
                _failCheckCts.Dispose();
                _failCheckCts = null;
            }
        }

        public void ClearCurrentLevel()
        {
            _isGameEnded = true;
            CancelFailCheckToken();
            UnsubscribeEvents();

            _levelGenerator.ClearLevel();
            if(_shooterQueue != null) _shooterQueue.ClearQueue();
            if(_slotManager != null) _slotManager.ClearSlots();
        }
    }
}
