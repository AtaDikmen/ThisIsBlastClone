using System;
using System.Collections.Generic;
using Block;
using Data;
using UnityEngine;

namespace Level
{
    [Serializable]
    public struct BlockColorEntry
    {
        public BlockType BlockType;
        public Color     Color;
    }

    public class LevelGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _gridBlockPrefab;

        [Header("Grid Layout Settings")]
        [SerializeField] private float _cellPadding = 0.08f;
        [SerializeField] private float _cellSize    = 0.25f;
        [SerializeField] private float _gridBottomY = -0.4f;

        [Header("Color Palette")]
        [SerializeField] private BlockColorEntry[] _colorPalette;

        private                 Dictionary<BlockType, Color> _colorLookup;
        private                 MaterialPropertyBlock        _propBlock;
        private readonly static int                          BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();

        public GameObject GridBlockPrefab  => _gridBlockPrefab;
        public LevelData  CurrentLevelData { get; private set; }

        public List<GridColumn> GenerateGrid(LevelData levelData)
        {
            CurrentLevelData = levelData;
            if(CurrentLevelData == null || _gridBlockPrefab == null)
            {
                Debug.LogError("[LevelGenerator] LevelData veya GridBlockPrefab atanmamış!", this);
                return new List<GridColumn>();
            }

            ClearLevel();
            BuildColorLookup();

            int rows    = CurrentLevelData.Row;
            int columns = CurrentLevelData.Column;

            float stepX = _cellSize + _cellPadding;
            float stepY = _cellSize + _cellPadding;

            float   totalWidth = columns * _cellSize + (columns - 1) * _cellPadding;
            float   originX    = -totalWidth / 2f + _cellSize / 2f;
            float   bottomY    = _gridBottomY;
            Vector3 stepOffset = new Vector3(0f, stepY, 0f);

            var gridColumns = new List<GridColumn>(columns);
            for(int c = 0; c < columns; c++)
            {
                var     col     = new GridColumn(c);
                float   colX    = originX + c * stepX;
                Vector3 basePos = new Vector3(colX, bottomY, 0f);
                col.SetPositionMapping(basePos, stepOffset);
                gridColumns.Add(col);
            }

            for(int r = rows - 1; r >= 0; r--)
            {
                int   rowIndexFromBottom = (rows - 1) - r;
                float y                  = bottomY + rowIndexFromBottom * stepY;

                for(int c = 0; c < columns; c++)
                {
                    BlockType cellType = CurrentLevelData.GetCell(r, c);
                    if(cellType == BlockType.Empty) continue;

                    float x        = originX + c * stepX;
                    var   worldPos = new Vector3(x, y, 0f);

                    var blockObj = Instantiate(_gridBlockPrefab, worldPos, Quaternion.identity, transform);
                    blockObj.name = $"GridBlock_{cellType}_{r}_{c}";

                    ApplyBlockColor(blockObj, cellType);

                    var gridBlock = blockObj.GetComponent<GridBlock>();
                    if(gridBlock == null)
                        gridBlock = blockObj.AddComponent<GridBlock>();

                    gridBlock.Setup(cellType, c);
                    gridColumns[c].AddBlock(gridBlock);
                    _spawnedBlocks.Add(blockObj);
                }
            }

            return gridColumns;
        }

        public void ClearLevel()
        {
            foreach(var block in _spawnedBlocks)
            {
                if(block != null) Destroy(block);
            }
            _spawnedBlocks.Clear();
        }

        public void BuildColorLookup()
        {
            _colorLookup = new Dictionary<BlockType, Color>
                           {
                               [BlockType.Red]           = new Color(0.90f, 0.20f, 0.20f),
                               [BlockType.Blue]          = new Color(0.20f, 0.45f, 0.90f),
                               [BlockType.Green]         = new Color(0.20f, 0.75f, 0.30f),
                               [BlockType.Yellow]        = new Color(0.98f, 0.82f, 0.10f),
                               [BlockType.Purple]        = new Color(0.60f, 0.20f, 0.85f),
                               [BlockType.Obstacle_Iron] = new Color(0.55f, 0.55f, 0.60f)
                           };

            if(_colorPalette != null && _colorPalette.Length > 0)
            {
                foreach(var entry in _colorPalette)
                    _colorLookup[entry.BlockType] = entry.Color;
            }
        }

        public void ApplyBlockColor(GameObject blockObj, BlockType blockType)
        {
            if(blockObj == null) return;

            var renderer = blockObj.GetComponentInChildren<Renderer>();
            if(renderer == null) return;

            if(_colorLookup == null) BuildColorLookup();
            if(_propBlock == null) _propBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_propBlock);

            if(_colorLookup.TryGetValue(blockType, out Color color))
                _propBlock.SetColor(BaseColorId, color);
            else
                _propBlock.SetColor(BaseColorId, Color.white);

            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
