using System;
using System.Collections.Generic;
using Data;
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
            foreach (var col in columns)
            {
                _columns.Add(col);
                col.OnFrontChanged  += _ => OnFrontRowChanged?.Invoke();
                col.OnColumnEmpty   += HandleColumnEmpty;
            }
        }

        public GridBlock GetFrontBlock(BlockType type)
        {
            foreach (var col in _columns)
            {
                if (!col.IsEmpty && col.FrontType == type)
                    return col.FrontBlock;
            }
            return null;
        }

        public bool HasMatchInFrontRow(BlockType type)
            => GetFrontBlock(type) != null;

        public bool IsAllEmpty()
        {
            foreach (var col in _columns)
                if (!col.IsEmpty) return false;
            return true;
        }

        private void HandleColumnEmpty(GridColumn col)
        {
            // Butun kolonlar bosaldiysa level tamamlandi
            if (IsAllEmpty())
            {
                Debug.Log("[GridManager] Tum bloklar patladi! Level tamamlandi.");
                OnLevelComplete?.Invoke();
            }
        }
    }
}
