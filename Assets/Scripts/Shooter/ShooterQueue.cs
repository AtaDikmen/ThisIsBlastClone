using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Level;
using UnityEngine;

namespace Shooter
{
    public class ShooterQueue : MonoBehaviour
    {
        [Tooltip("Kolon (Lane) sayisi.")]
        [SerializeField] private int _laneCount = 3;

        [Tooltip("Kolonlar arasi yatay bosluk.")]
        [SerializeField] private float _queueHorizontalPadding = 0.1f;

        [Tooltip("Kuyruk satirlari arasi dikey bosluk.")]
        [SerializeField] private float _queueVerticalPadding = 0.08f;

        [Tooltip("Kuyruk bloklarinin boyutu (Scale).")]
        [SerializeField] private float _queueBlockSize = 0.2f;

        [Tooltip("En on siradaki (secilebilir) ilk siranin duracagi SABIT dikey Y pozisyonu.")]
        [SerializeField] private float _queueFrontY = -4f;

        [Tooltip("Kuyrugun yatay merkez X pozisyonu.")]
        [SerializeField] private float _queueCenterX = 0f;

        private List<ShooterBlock>[] _lanes;

        public event Action<ShooterBlock> OnBlockSelected;
        public event Action               OnQueueEmpty;

        public void ConfigureLayout(float frontY, float horizontalPadding, float verticalPadding, float blockSize)
        {
            _queueFrontY            = frontY;
            _queueHorizontalPadding = horizontalPadding;
            _queueVerticalPadding   = verticalPadding;
            _queueBlockSize         = blockSize;
        }

        public void InitializeQueue(LevelData levelData, GameObject prefab, Action<GameObject, BlockType> applyColorCallback)
        {
            ClearQueue();

            if(levelData == null || prefab == null) return;
            if(levelData.ShooterBlocks == null || levelData.ShooterBlocks.Length == 0)
            {
                Debug.LogWarning("[ShooterQueue] LevelData icinde ShooterBlocks tanimi bulunamadi!");
                return;
            }

            _laneCount = Mathf.Max(1, levelData.SlotCount);
            _lanes     = new List<ShooterBlock>[_laneCount];
            for(int i = 0; i < _laneCount; i++)
            {
                _lanes[i] = new List<ShooterBlock>();
            }

            int count = levelData.ShooterBlocks.Length;
            for(int i = 0; i < count; i++)
            {
                var entry = levelData.ShooterBlocks[i];
                if(entry.Type == BlockType.Empty) continue;

                var blockObj = Instantiate(prefab, transform);
                if(blockObj == null) continue;

                blockObj.name = $"ShooterBlock_{entry.Type}_{i}";

                applyColorCallback?.Invoke(blockObj, entry.Type);

                var shooterComp = blockObj.GetComponent<ShooterBlock>();
                if(shooterComp == null)
                    shooterComp = blockObj.AddComponent<ShooterBlock>();

                if(shooterComp != null)
                {
                    shooterComp.Setup(entry.Type, entry.BulletCount);
                    shooterComp.OnTapped += HandleBlockTapped;

                    int targetLane = i % _laneCount;
                    _lanes[targetLane].Add(shooterComp);
                }
            }

            UpdateAllLanePositions(instant: true);
        }

        private void HandleBlockTapped(ShooterBlock block)
        {
            if(block == null || block.IsInSlot) return;

            if(IsFrontBlock(block))
            {
                OnBlockSelected?.Invoke(block);
            }
            else
            {
                Debug.Log($"[ShooterQueue] '{block.name}' henuz arka sirada, on siraya gelmesi gerekiyor.");
            }
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

                    UpdateLanePositions(l, instant: false);
                    break;
                }
            }

            if(IsAllEmpty())
            {
                OnQueueEmpty?.Invoke();
            }
        }

        private bool IsAllEmpty()
        {
            if(_lanes == null) return true;
            for(int l = 0; l < _lanes.Length; l++)
            {
                if(_lanes[l].Count > 0) return false;
            }
            return true;
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

            float stepX = _queueBlockSize + _queueHorizontalPadding;
            float stepY = _queueBlockSize + _queueVerticalPadding;

            float totalWidth = laneCount * _queueBlockSize + (laneCount - 1) * _queueHorizontalPadding;
            float startX     = _queueCenterX - (totalWidth / 2f) + (_queueBlockSize / 2f);
            float laneX      = startX + laneIndex * stepX;

            for(int row = 0; row < lane.Count; row++)
            {
                var block = lane[row];
                if(block == null) continue;

                float y = _queueFrontY - (row * stepY);
                float z = 0f;

                var targetPos = new Vector3(laneX, y, z);

                if(instant)
                    block.transform.position = targetPos;
                else
                    StartCoroutine(SmoothMoveBlock(block, targetPos));
            }
        }

        private IEnumerator SmoothMoveBlock(ShooterBlock block, Vector3 targetPos)
        {
            if(block == null) yield break;

            Vector3 startPos = block.transform.position;
            float   duration = 0.16f;
            float   elapsed  = 0f;

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
                block.transform.position = targetPos;
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
    }
}
