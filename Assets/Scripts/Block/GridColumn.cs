using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Block
{
    public class GridColumn
    {
        private readonly List<GridBlock> _blocks = new List<GridBlock>();

        public int ColumnIndex { get; }
        private Vector3 _basePosition;
        private Vector3 _stepOffset = Vector3.up * 0.3f;
        private bool _isMappingSet;

        public event Action<GridColumn> OnFrontChanged;
        public event Action<GridColumn> OnColumnEmpty;

        public GridBlock FrontBlock => _blocks.Count > 0 ? _blocks[0] : null;
        public BlockType FrontType  => FrontBlock?.Type ?? BlockType.Empty;
        public bool IsEmpty         => _blocks.Count == 0;
        public int Count            => _blocks.Count;

        public GridColumn(int columnIndex)
        {
            ColumnIndex = columnIndex;
        }

        public void SetPositionMapping(Vector3 basePosition, Vector3 stepOffset)
        {
            _basePosition = basePosition;
            _stepOffset   = stepOffset;
            _isMappingSet = true;
        }

        public void AddBlock(GridBlock block)
        {
            block.OnExploded += HandleBlockExploded;
            _blocks.Add(block);
        }

        public GridBlock GetBlockAtRow(int rowIndex)
        {
            foreach (var block in _blocks)
            {
                if (block != null && block.RowIndex == rowIndex)
                    return block;
            }
            return null;
        }

        private void HandleBlockExploded(GridBlock explodedBlock)
        {
            explodedBlock.OnExploded -= HandleBlockExploded;
            _blocks.Remove(explodedBlock);

            RefreshBlockPositions();

            if (IsEmpty)
                OnColumnEmpty?.Invoke(this);
            else
                OnFrontChanged?.Invoke(this);
        }

        private void RefreshBlockPositions()
        {
            if (!_isMappingSet) return;

            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                if (block != null)
                {
                    Vector3 targetPos = _basePosition + (float)i * _stepOffset;
                    block.SlideTo(targetPos, 0.12f);
                }
            }
        }
    }
}