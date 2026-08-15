using System;
using System.Collections;
using Block;
using Data;
using UnityEngine;

namespace Shooter
{
    public class ShooterSlot : MonoBehaviour
    {
        public ShooterBlock OccupiedBy { get; private set; }
        public bool         IsOccupied => OccupiedBy != null;
        public bool         CanFire    { get; private set; }

        public event Action<ShooterSlot> OnSlotFreed;

        private GridManager                   _gridManager;
        private GameObject                    _projectilePrefab;
        private Action<GameObject, BlockType> _applyColorCallback;
        private Coroutine                     _fireLoopRoutine;

        public void PlaceAndFire(ShooterBlock block, GridManager gridManager, GameObject projectilePrefab, Action<GameObject, BlockType> applyColorCallback)
        {
            if(IsOccupied)
            {
                Debug.LogWarning($"[ShooterSlot] '{name}' zaten dolu!");
                return;
            }

            _gridManager        = gridManager;
            _projectilePrefab   = projectilePrefab;
            _applyColorCallback = applyColorCallback;
            OccupiedBy          = block;
            OccupiedBy.IsInSlot = true;

            block.OnEmpty += HandleBlockEmpty;
            block.OnFired += HandleBlockFired;

            if(_gridManager != null)
                _gridManager.OnFrontRowChanged += TryFire;

            StartCoroutine(MoveToSlotAndStartFiring(block));
        }

        private IEnumerator MoveToSlotAndStartFiring(ShooterBlock block)
        {
            Vector3 startPos  = block.transform.position;
            Vector3 targetPos = transform.position;
            float   duration  = 0.18f;
            float   elapsed   = 0f;

            while(elapsed < duration)
            {
                if(block == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t                        = Mathf.Sin(t * Mathf.PI * 0.5f);
                block.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if(block != null)
            {
                block.transform.position = targetPos;
                block.transform.SetParent(transform);
            }

            TryFire();
        }

        public void TryFire()
        {
            if(!IsOccupied || OccupiedBy == null || _gridManager == null) return;
            if(OccupiedBy.IsFiring || OccupiedBy.IsEmpty) return;

            var target = _gridManager.GetFrontBlock(OccupiedBy.Type);

            if(target != null)
            {
                CanFire = true;
                OccupiedBy.FireProjectileAt(target, _projectilePrefab, _applyColorCallback);
            }
            else
            {
                CanFire = false;
            }
        }

        private void HandleBlockFired(ShooterBlock block)
        {
            if(gameObject.activeInHierarchy)
                StartCoroutine(DelayedNextShotCheck());
        }

        private IEnumerator DelayedNextShotCheck()
        {
            yield return new WaitForSeconds(0.08f);
            TryFire();
        }

        private void HandleBlockEmpty(ShooterBlock block)
        {
            if(block != null)
            {
                block.OnEmpty -= HandleBlockEmpty;
                block.OnFired -= HandleBlockFired;
            }

            if(_gridManager != null)
                _gridManager.OnFrontRowChanged -= TryFire;

            OccupiedBy = null;
            CanFire    = false;

            OnSlotFreed?.Invoke(this);
        }
    }
}
