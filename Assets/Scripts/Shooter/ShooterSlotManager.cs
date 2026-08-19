using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Block;
using Cysharp.Threading.Tasks;
using Data;
using Level;
using PrimeTween;
using Services;
using UnityEngine;
using VContainer;

namespace Shooter
{
    public class ShooterSlotManager : MonoBehaviour
    {
        [Header("Slot Layout")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private float     slotSpacing         = 1.1f;
        [SerializeField] private Vector3   slotsOriginPosition = new Vector3(0f, -0.8f, 0f);

        [Header("Projectiles")]
        [SerializeField] private GameObject projectilePrefab;

        private readonly List<ShooterSlot> _slots     = new List<ShooterSlot>();
        private readonly SemaphoreSlim     _mergeLock = new SemaphoreSlim(1, 1);

        private GridManager       _gridManager;
        private ShooterQueue      _shooterQueue;
        private LevelGenerator    _levelGenerator;
        private IAudioService     _audioService;
        private IVibrationService _vibrationService;

        [Inject]
        public void Construct(
            GridManager       gridManager,
            ShooterQueue      shooterQueue,
            LevelGenerator    levelGenerator,
            IAudioService     audioService,
            IVibrationService vibrationService)
        {
            _gridManager      = gridManager;
            _shooterQueue     = shooterQueue;
            _levelGenerator   = levelGenerator;
            _audioService     = audioService;
            _vibrationService = vibrationService;
        }

        private void OnDestroy()
        {
            if(_shooterQueue != null)
                _shooterQueue.OnBlockSelected -= HandleBlockSelected;

            _mergeLock?.Dispose();
            ClearSlots();
        }

        public void InitializeSlots(int slotCount)
        {
            ClearSlots();

            if(slotPrefab == null)
            {
                Debug.LogError("[ShooterSlotManager] Slot Prefab missing!", this);
                return;
            }

            float totalWidth = (slotCount - 1) * slotSpacing;
            float startX     = slotsOriginPosition.x - (totalWidth / 2f);

            for(int i = 0; i < slotCount; i++)
            {
                var slotPos = new Vector3(startX + (i * slotSpacing), slotsOriginPosition.y, slotsOriginPosition.z);
                var parent  = slotsParent != null ? slotsParent : transform;

                var slotObj = Instantiate(slotPrefab, slotPos, Quaternion.identity, parent);
                slotObj.name = $"ShooterSlot_{i}";

                var slotComp                  = slotObj.GetComponent<ShooterSlot>();
                if(slotComp == null) slotComp = slotObj.AddComponent<ShooterSlot>();

                slotComp.OnSlotFreed += HandleSlotFreed;
                _slots.Add(slotComp);
            }

            if(_shooterQueue != null)
            {
                _shooterQueue.OnBlockSelected -= HandleBlockSelected;
                _shooterQueue.OnBlockSelected += HandleBlockSelected;
            }
        }

        private void HandleBlockSelected(ShooterBlock block)
        {
            ShooterSlot availableSlot = GetFirstEmptySlot();
            if(availableSlot == null) return;

            _shooterQueue.RemoveFromQueue(block);
            MoveAndActivateSlotAsync(availableSlot, block).Forget();
        }

        private async UniTaskVoid MoveAndActivateSlotAsync(ShooterSlot slot, ShooterBlock block)
        {
            await slot.PlaceAndAnimateAsync(block, _gridManager, this, projectilePrefab, _levelGenerator.ApplyBlockColor);

            _audioService?.PlaySFX(SoundType.ShooterSlotLand);

            bool didMerge = await TryMergeSlotsAsync(block.Type);
            if(!didMerge && slot.IsOccupied && slot.OccupiedBy != null)
            {
                slot.StartFiringSequence();
            }
        }

        private async UniTask<bool> TryMergeSlotsAsync(BlockType targetType)
        {
            await _mergeLock.WaitAsync();

            try
            {
                var matchingSlots = new List<ShooterSlot>();

                foreach(var slot in _slots)
                {
                    if(slot.IsOccupied && slot.OccupiedBy != null && slot.OccupiedBy.Type == targetType && slot.OccupiedBy.CanBeMerged)
                        matchingSlots.Add(slot);
                }

                if(matchingSlots.Count < 3) return false;

                var mainSlot   = matchingSlots[0];
                var secondSlot = matchingSlots[1];
                var thirdSlot  = matchingSlots[2];

                var mainBlock   = mainSlot.OccupiedBy;
                var secondBlock = secondSlot.OccupiedBy;
                var thirdBlock  = thirdSlot.OccupiedBy;

                if(mainBlock == null || secondBlock == null || thirdBlock == null) return false;
                if(!mainBlock.CanBeMerged || !secondBlock.CanBeMerged || !thirdBlock.CanBeMerged) return false;

                mainBlock.IsMerging   = true;
                secondBlock.IsMerging = true;
                thirdBlock.IsMerging  = true;

                secondBlock.IsEscaping = true;
                thirdBlock.IsEscaping  = true;

                mainSlot.StopFiringSequence();
                secondSlot.StopFiringSequence();
                thirdSlot.StopFiringSequence();

                var absorbSequence = Sequence.Create()
                                             .Group(Tween.Position(secondBlock.transform, mainBlock.transform.position, duration: 0.20f, ease: Ease.InOutCubic))
                                             .Group(Tween.Scale(secondBlock.transform, Vector3.zero, duration: 0.20f, ease: Ease.InSine))
                                             .Group(Tween.Position(thirdBlock.transform, mainBlock.transform.position, duration: 0.20f, ease: Ease.InOutCubic))
                                             .Group(Tween.Scale(thirdBlock.transform, Vector3.zero, duration: 0.20f, ease: Ease.InSine));

                await absorbSequence.ToYieldInstruction();

                if(mainBlock == null) return false;

                int secondBullets = secondBlock.BulletCount;
                int thirdBullets  = thirdBlock.BulletCount;

                mainBlock.SetBulletCount(mainBlock.BulletCount + secondBullets + thirdBullets);

                secondSlot.ClearSlotReferenceForMerge();
                thirdSlot.ClearSlotReferenceForMerge();

                Destroy(secondBlock.gameObject);
                Destroy(thirdBlock.gameObject);

                secondSlot.NotifySlotFreed();
                thirdSlot.NotifySlotFreed();

                mainBlock.IsMerging = false;

                await mainBlock.PlayMergeJuiceAsync();
                _vibrationService?.VibrateMedium();
                mainSlot.StartFiringSequence();
                return true;
            }
            finally
            {
                _mergeLock.Release();
            }
        }

        private ShooterSlot GetFirstEmptySlot()
        {
            foreach(var slot in _slots)
            {
                if(!slot.IsOccupied) return slot;
            }
            return null;
        }

        private void HandleSlotFreed(ShooterSlot slot)
        {
        }

        public bool IsLowestAmmoShooterForColor(ShooterSlot currentSlot, BlockType color)
        {
            if(currentSlot == null || !currentSlot.IsOccupied || currentSlot.OccupiedBy == null)
                return false;

            int currentAmmo = currentSlot.OccupiedBy.BulletCount;

            foreach(var slot in _slots)
            {
                if(slot == currentSlot) continue;

                if(slot.IsOccupied && slot.OccupiedBy != null && !slot.OccupiedBy.IsEmpty && slot.OccupiedBy.Type == color)
                {
                    int otherAmmo = slot.OccupiedBy.BulletCount;

                    if(otherAmmo < currentAmmo)
                        return false;
                    if(otherAmmo == currentAmmo)
                    {
                        int currentIndex = _slots.IndexOf(currentSlot);
                        int otherIndex   = _slots.IndexOf(slot);
                        if(otherIndex < currentIndex)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool IsAllSlotsFull()
        {
            foreach(var slot in _slots)
            {
                if(!slot.IsOccupied) return false;
            }
            return true;
        }

        public void ClearSlots()
        {
            foreach(var slot in _slots)
            {
                if(slot != null)
                {
                    slot.OnSlotFreed -= HandleSlotFreed;
                    Destroy(slot.gameObject);
                }
            }
            _slots.Clear();
        }

        public bool HasAnyValidTargetOnGrid(GridManager gridManager)
        {
            if(gridManager == null) return false;

            foreach(var slot in _slots)
            {
                if(slot.IsOccupied && slot.OccupiedBy != null && !slot.OccupiedBy.IsEmpty)
                {
                    var target = gridManager.GetAvailableFrontBlock(slot.OccupiedBy.Type);
                    if(target != null)
                        return true;
                }
            }

            return false;
        }

        public bool IsAnyShooterFiring()
        {
            foreach(var slot in _slots)
            {
                if(slot.IsOccupied && slot.OccupiedBy != null && slot.OccupiedBy.IsFiring)
                    return true;
            }
            return false;
        }

        public void TriggerAllSlotsToFire()
        {
            foreach(var slot in _slots)
            {
                if(slot != null && slot.IsOccupied)
                    slot.StartFiringSequence();
            }
        }

        public void ClearSlotsWithRunAwayAnimation()
        {
            foreach(var slot in _slots)
            {
                if(slot != null && slot.IsOccupied && slot.OccupiedBy != null)
                {
                    var block = slot.OccupiedBy;
                    slot.ClearSlotReferenceForMerge();
                    block.PlayRunAwayAndDestroyAsync().Forget();
                    slot.NotifySlotFreed();
                }
            }
        }
    }
}
