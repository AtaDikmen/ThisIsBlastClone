using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using PrimeTween;
using UnityEngine;

namespace Block
{
    public class GridManager : MonoBehaviour
    {
        private readonly List<GridColumn> _columns = new List<GridColumn>();

        public event Action OnLevelComplete;
        public event Action OnFrontRowChanged;

        public void RegisterColumns(IEnumerable<GridColumn> columns)
        {
            _columns.Clear();
            foreach(var col in columns)
            {
                _columns.Add(col);
                col.OnFrontChanged += _ => OnFrontRowChanged?.Invoke();
                col.OnColumnEmpty  += HandleColumnEmpty;
            }
        }

        public GridBlock GetAvailableFrontBlock(BlockType shooterType)
        {
            foreach(var col in _columns)
            {
                if(!col.IsEmpty)
                {
                    var frontBlock = col.FrontBlock;
                    if(frontBlock != null && !frontBlock.IsTargeted && !frontBlock.IsExploding)
                    {
                        if(frontBlock.Type == shooterType || frontBlock.IsRainbow || frontBlock.IsBomb)
                            return frontBlock;
                    }
                }
            }
            return null;
        }

        public async UniTaskVoid ExplodeNeighborsAsync(int colIndex, int rowIndex)
        {
            if(Camera.main != null)
                Tween.ShakeLocalPosition(Camera.main.transform, new Vector3(0.12f, 0.12f, 0f), duration: 0.18f, frequency: 30);

            var targetBlocks = new List<GridBlock>();

            for(int c = colIndex - 1; c <= colIndex + 1; c++)
            {
                if(c < 0 || c >= _columns.Count) continue;

                var column = _columns[c];

                for(int r = rowIndex - 1; r <= rowIndex + 1; r++)
                {
                    if(c == colIndex && r == rowIndex) continue;

                    var targetBlock = column.GetBlockAtRow(r);
                    if(targetBlock != null && !targetBlock.IsExploding)
                        targetBlocks.Add(targetBlock);
                }
            }

            foreach(var block in targetBlocks)
            {
                if(block != null && !block.IsExploding)
                {
                    block.TakeDamage(999);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.035f));
                }
            }
        }

        public bool IsAllEmpty()
        {
            foreach(var col in _columns)
                if(!col.IsEmpty)
                    return false;
            return true;
        }

        private void HandleColumnEmpty(GridColumn col)
        {
            if(IsAllEmpty())
            {
                Debug.Log("[GridManager] Tüm bloklar temizlendi! Level tamamlandı.");
                OnLevelComplete?.Invoke();
            }
        }
    }
}
