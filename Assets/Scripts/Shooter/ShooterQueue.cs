using System;
using System.Collections.Generic;
using Audio;
using Cysharp.Threading.Tasks;
using Data;
using Level;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace Shooter
{
    public class ShooterQueue : MonoBehaviour
    {
        [Header("Shooter Prefab")]
        [SerializeField] private GameObject shooterBlockPrefab;

        [Header("Layout Settings")]
        [SerializeField, Range(1, 5)] private int laneCount = 5;
        [SerializeField] private float   queueHorizontalPadding = 0.5f;
        [SerializeField] private float   queueVerticalPadding   = 0.5f;
        [SerializeField] private float   queueBlockSize         = 1f;
        [SerializeField] private Vector3 queueOriginPosition    = new Vector3(0f, -1.8f, 0f);

        [Header("Outline Material")]
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField] private float outlineWidth = 0.05f;

        private List<ShooterBlock>[]      _lanes;
        public event Action<ShooterBlock> OnBlockSelected;

        private IAudioService _audioService;

        [Inject]
        public void Construct(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public void InitializeQueue(LevelData levelData, Action<GameObject, BlockType> applyColorCallback)
        {
            ClearQueue();

            if(levelData == null || shooterBlockPrefab == null)
            {
                Debug.LogError("[ShooterQueue] LevelData or ShooterBlockPrefab is missing!", this);
                return;
            }

            laneCount = Mathf.Max(1, levelData.SlotCount);
            _lanes    = new List<ShooterBlock>[laneCount];
            for(int i = 0; i < laneCount; i++)
            {
                _lanes[i] = new List<ShooterBlock>();
            }

            int count = levelData.ShooterBlocks.Length;
            for(int i = 0; i < count; i++)
            {
                var entry = levelData.ShooterBlocks[i];
                if(entry.Type == BlockType.Empty) continue;

                var blockObj = Instantiate(shooterBlockPrefab, transform);
                blockObj.name = $"ShooterBlock_{entry.Type}_{i}";

                applyColorCallback?.Invoke(blockObj, entry.Type);

                var shooterComp = blockObj.GetComponent<ShooterBlock>();
                if(shooterComp == null)
                    shooterComp = blockObj.AddComponent<ShooterBlock>();

                int ammo = entry.BulletCount;
                shooterComp.Setup(entry.Type, ammo, _audioService);
                shooterComp.OnTapped += HandleBlockTapped;

                int targetLane = i % laneCount;
                _lanes[targetLane].Add(shooterComp);
            }

            UpdateAllLanePositions(instant: true);
        }

        private void HandleBlockTapped(ShooterBlock block)
        {
            if(block == null || block.IsInSlot) return;

            if(IsFrontBlock(block))
                OnBlockSelected?.Invoke(block);
        }

        public bool IsFrontBlock(ShooterBlock block)
        {
            if(_lanes == null) return false;

            for(int l = 0; l < _lanes.Length; l++)
            {
                if(_lanes[l].Count > 0 && _lanes[l][0] == block)
                    return true;
            }
            return false;
        }

        public void RemoveFromQueue(ShooterBlock block)
        {
            if(_lanes == null) return;

            for(int l = 0; l < _lanes.Length; l++)
            {
                if(_lanes[l].Remove(block))
                {
                    block.OnTapped -= HandleBlockTapped;
                    block.IsInSlot =  true;

                    block.RemoveOutline();

                    UpdateLanePositions(l, instant: false);
                    break;
                }
            }
        }

        public void UpdateAllLanePositions(bool instant = false)
        {
            if(_lanes == null) return;
            for(int l = 0; l < _lanes.Length; l++)
            {
                UpdateLanePositions(l, instant);
            }
        }

        private void UpdateLanePositions(int laneIndex, bool instant)
        {
            if(_lanes == null || laneIndex >= _lanes.Length) return;

            var lane      = _lanes[laneIndex];
            int laneCount = _lanes.Length;

            float stepX = queueBlockSize + queueHorizontalPadding;
            float stepY = queueBlockSize + queueVerticalPadding;

            float totalWidth = laneCount * queueBlockSize + (laneCount - 1) * queueHorizontalPadding;
            float startX     = queueOriginPosition.x - (totalWidth / 2f) + (queueBlockSize / 2f);
            float laneX      = startX + laneIndex * stepX;

            for(int row = 0; row < lane.Count; row++)
            {
                var block = lane[row];
                if(block == null) continue;

                if(row == 0)
                    block.ApplyOutline(outlineMaterial, outlineColor, outlineWidth);
                else
                    block.RemoveOutline();

                float y         = queueOriginPosition.y - (row * stepY);
                var   targetPos = new Vector3(laneX, y, queueOriginPosition.z);

                if(instant)
                    block.transform.position = targetPos;
                else
                    Tween.Position(block.transform, targetPos, duration: 0.16f, ease: Ease.OutQuad);
            }
        }

        public void ClearQueue()
        {
            if(_lanes == null) return;

            for(int l = 0; l < _lanes.Length; l++)
            {
                foreach(var block in _lanes[l])
                {
                    if(block != null)
                    {
                        block.OnTapped -= HandleBlockTapped;
                        Destroy(block.gameObject);
                    }
                }
                _lanes[l].Clear();
            }
        }

        public void ClearQueueWithRunAwayAnimation()
        {
            if(_lanes == null) return;

            for(int l = 0; l < _lanes.Length; l++)
            {
                foreach(var block in _lanes[l])
                {
                    if(block != null)
                    {
                        block.OnTapped -= HandleBlockTapped;
                        block.PlayRunAwayAndDestroyAsync().Forget();
                    }
                }
                _lanes[l].Clear();
            }
        }
    }
}
