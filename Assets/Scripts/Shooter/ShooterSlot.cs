using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Block;
using Data;

namespace Shooter
{
    public class ShooterSlot : MonoBehaviour
    {
        public ShooterBlock OccupiedBy { get; private set; }
        public bool IsOccupied => OccupiedBy != null;

        public event Action<ShooterSlot> OnSlotFreed;

        private GridManager _gridManager;
        private GameObject _projectilePrefab;
        private Action<GameObject, BlockType> _applyColorCallback;

        private readonly SemaphoreSlim _placementLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _placementLock?.Dispose();
        }

        /// <summary>
        /// Bloğu slota taşır, yerleşme biter bitmez Merge kontrolünün yapılmasına izin verir, ardından ateş etmeyi başlatır.
        /// </summary>
        public async UniTask PlaceAndAnimateAsync(
            ShooterBlock block, 
            GridManager gridManager, 
            GameObject projectilePrefab, 
            Action<GameObject, BlockType> applyColorCallback)
        {
            await _placementLock.WaitAsync(_cts.Token);

            try
            {
                _gridManager = gridManager;
                _projectilePrefab = projectilePrefab;
                _applyColorCallback = applyColorCallback;

                OccupiedBy = block;
                OccupiedBy.IsInSlot = true;

                // Slota yerleşme animasyonu (EaseOutQuad)
                await AnimateToSlotAsync(block.transform, transform.position, 0.16f, _cts.Token);

                if (block != null)
                    block.transform.SetParent(transform);

                if (_gridManager != null)
                    _gridManager.OnFrontRowChanged += OnFrontRowChangedHandler;
            }
            finally
            {
                _placementLock.Release();
            }
        }

        /// <summary>
        /// Merge kontrolü tamamlandıktan sonra dışarıdan tetiklenerek ateş etmeyi başlatır.
        /// </summary>
        public void StartFiringSequence()
        {
            CheckAndFireSequenceAsync(_cts.Token).Forget();
        }

        private void OnFrontRowChangedHandler()
        {
            CheckAndFireSequenceAsync(_cts.Token).Forget();
        }

        private async UniTask CheckAndFireSequenceAsync(CancellationToken ct)
        {
            if (!IsOccupied || OccupiedBy == null || _gridManager == null) return;
            if (OccupiedBy.IsFiring || OccupiedBy.IsEmpty) return;

            var target = _gridManager.GetFrontBlock(OccupiedBy.Type);
            if (target != null)
            {
                OccupiedBy.SetFiringState(true);

                await FireProjectileTaskAsync(target, ct);

                if (OccupiedBy == null) return;

                OccupiedBy.DecreaseBulletCount();

                if (OccupiedBy.IsEmpty)
                {
                    HandleEmpty();
                }
                else
                {
                    OccupiedBy.SetFiringState(false);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.06f), cancellationToken: ct);
                    await CheckAndFireSequenceAsync(ct);
                }
            }
        }

        private async UniTask AnimateToSlotAsync(Transform target, Vector3 dest, float duration, CancellationToken ct)
        {
            if (target == null) return;
            Vector3 startPos = target.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                if (target == null) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Sin((elapsed / duration) * Mathf.PI * 0.5f);
                target.position = Vector3.Lerp(startPos, dest, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (target != null)
                target.position = dest;
        }

        private async UniTask FireProjectileTaskAsync(GridBlock target, CancellationToken ct)
        {
            var utcs = new UniTaskCompletionSource();

            OccupiedBy.FireProjectileAt(target, _projectilePrefab, _applyColorCallback, () =>
            {
                utcs.TrySetResult();
            });

            await utcs.Task;
        }

        public void ClearSlotReferenceForMerge()
        {
            if (_gridManager != null)
                _gridManager.OnFrontRowChanged -= OnFrontRowChangedHandler;

            OccupiedBy = null;
        }

        public void NotifySlotFreed()
        {
            OnSlotFreed?.Invoke(this);
        }

        private void HandleEmpty()
        {
            if (_gridManager != null)
                _gridManager.OnFrontRowChanged -= OnFrontRowChangedHandler;

            var tempBlock = OccupiedBy;
            OccupiedBy = null;

            if (tempBlock != null)
                Destroy(tempBlock.gameObject);

            OnSlotFreed?.Invoke(this);
        }
    }
}