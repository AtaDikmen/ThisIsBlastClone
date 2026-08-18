using System;
using System.Threading;
using Block;
using Cysharp.Threading.Tasks;
using Data;
using PrimeTween;
using UnityEngine;

namespace Shooter
{
    public class ShooterSlot : MonoBehaviour
    {
        public ShooterBlock OccupiedBy { get; private set; }
        public bool         IsOccupied => OccupiedBy != null;

        public event Action<ShooterSlot> OnSlotFreed;

        private GridManager                   _gridManager;
        private ShooterSlotManager            _slotManager;
        private GameObject                    _projectilePrefab;
        private Action<GameObject, BlockType> _applyColorCallback;

        private readonly SemaphoreSlim           _placementLock = new SemaphoreSlim(1, 1);
        private          CancellationTokenSource _cts;

        private void Awake()
        {
            ResetCancellationTokenSource();
        }

        private void OnDestroy()
        {
            UnsubscribeGridEvents();

            if(_cts != null)
            {
                if(!_cts.IsCancellationRequested)
                    _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _placementLock?.Dispose();
        }

        private void ResetCancellationTokenSource()
        {
            if(_cts != null)
            {
                if(!_cts.IsCancellationRequested)
                    _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
        }

        private void UnsubscribeGridEvents()
        {
            if(_gridManager != null)
                _gridManager.OnFrontRowChanged -= OnFrontRowChangedHandler;
        }

        public async UniTask PlaceAndAnimateAsync(
            ShooterBlock                  block,
            GridManager                   gridManager,
            ShooterSlotManager            slotManager,
            GameObject                    projectilePrefab,
            Action<GameObject, BlockType> applyColorCallback)
        {
            if(_cts == null || _cts.IsCancellationRequested)
                ResetCancellationTokenSource();

            await _placementLock.WaitAsync(_cts.Token);

            try
            {
                _gridManager        = gridManager;
                _slotManager        = slotManager;
                _projectilePrefab   = projectilePrefab;
                _applyColorCallback = applyColorCallback;

                OccupiedBy          = block;
                OccupiedBy.IsInSlot = true;

                await AnimateToSlotAsync(block.transform, transform.position, _cts.Token);

                if(block != null)
                {
                    block.transform.SetParent(transform);
                    block.transform.localPosition = Vector3.zero;
                    block.transform.localRotation = Quaternion.identity;
                }
            }
            finally
            {
                _placementLock?.Release();
            }
        }

        private async UniTask AnimateToSlotAsync(Transform target, Vector3 destination, CancellationToken ct)
        {
            if(target == null) return;

            Vector3 startPos   = target.position;
            Vector3 midPoint   = Vector3.Lerp(startPos, destination, 0.5f);
            float   sideOffset = (startPos.x < destination.x) ? 0.35f : -0.35f;
            midPoint.x += sideOffset;

            midPoint.z = destination.z;

            var sequence = Sequence.Create()
                                   .Group(Tween.Position(target, midPoint, duration: 0.12f, ease: Ease.OutQuad))
                                   .Group(Tween.PunchScale(target, new Vector3(0.15f, -0.15f, 0f), duration: 0.24f, frequency: 3))
                                   .Chain(Tween.Position(target, destination, duration: 0.12f, ease: Ease.InQuad));

            await sequence.ToYieldInstruction();

            if(target != null)
                target.position = destination;
        }

        public void StartFiringSequence()
        {
            if(_gridManager != null)
            {
                _gridManager.OnFrontRowChanged -= OnFrontRowChangedHandler;
                _gridManager.OnFrontRowChanged += OnFrontRowChangedHandler;
            }

            TriggerFireSequence();
        }

        public void StopFiringSequence()
        {
            UnsubscribeGridEvents();
        }

        private void OnFrontRowChangedHandler()
        {
            TriggerFireSequence();
        }

        private void TriggerFireSequence()
        {
            if(_cts == null || _cts.IsCancellationRequested) return;

            CheckAndFireSequenceAsync(_cts.Token).Forget();
        }

        private async UniTask CheckAndFireSequenceAsync(CancellationToken ct)
        {
            if(ct.IsCancellationRequested) return;
            if(!IsOccupied || OccupiedBy == null || _gridManager == null) return;
            if(OccupiedBy.IsFiring || OccupiedBy.IsEmpty) return;

            if(_slotManager != null && !_slotManager.IsLowestAmmoShooterForColor(this, OccupiedBy.Type))
                return;

            var target = _gridManager.GetAvailableFrontBlock(OccupiedBy.Type);
            if(target != null)
            {
                OccupiedBy.SetFiringState(true);

                await FireProjectileTaskAsync(target, ct);

                if(ct.IsCancellationRequested || OccupiedBy == null) return;

                OccupiedBy.DecreaseBulletCount();

                if(OccupiedBy.IsEmpty)
                {
                    HandleEmpty();
                }
                else
                {
                    OccupiedBy.SetFiringState(false);

                    await UniTask.Delay(TimeSpan.FromSeconds(0.038f), cancellationToken: ct);

                    if(!ct.IsCancellationRequested)
                        CheckAndFireSequenceAsync(ct).Forget();
                }
            }
            else
                OccupiedBy.ResetOrientationToDefaultAsync().Forget();
        }

        private async UniTask FireProjectileTaskAsync(GridBlock target, CancellationToken ct)
        {
            var utcs = new UniTaskCompletionSource();
            OccupiedBy.FireProjectileAt(target, _projectilePrefab, _applyColorCallback, () => { utcs.TrySetResult(); });
            await utcs.Task;
        }

        public void ClearSlotReferenceForMerge()
        {
            UnsubscribeGridEvents();
            OccupiedBy = null;
        }

        public void NotifySlotFreed()
        {
            OnSlotFreed?.Invoke(this);
        }

        private void HandleEmpty()
        {
            UnsubscribeGridEvents();

            var escapingBlock = OccupiedBy;
            OccupiedBy = null;

            OnSlotFreed?.Invoke(this);

            if(_slotManager != null)
                _slotManager.TriggerAllSlotsToFire();

            if(escapingBlock != null)
                escapingBlock.PlayRunAwayAndDestroyAsync().Forget();
        }
    }
}
