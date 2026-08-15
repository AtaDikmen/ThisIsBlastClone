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
        [SerializeField] private LevelData _levelData;

        [SerializeField] private GameObject _blockPrefab;

        [Tooltip("Bloklar arasi bosluk (birim).")]
        [SerializeField] private float _cellPadding = 0.1f;

        [Tooltip("Tek bir blogun dunya boyutu (Scale).")]
        [SerializeField] private float _cellSize = 0.2f;

        [Tooltip("Grid'in en alt satırının (Front Row) duracağı SABİT dikey Y pozisyonu. Satır sayısı artsa da en alt sıra hep bu hizada kalır, fazlalıklar yukarı uzar.")]
        [SerializeField] private float _gridBottomY = -0.4f;

        [Header("Renk Paleti")]
        [SerializeField] private BlockColorEntry[] _colorPalette;

        // Cache
        private                 Dictionary<BlockType, Color> _colorLookup;
        private                 MaterialPropertyBlock        _propBlock;
        private readonly static int                          _baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();

        public GameObject BlockPrefab      => _blockPrefab;
        public LevelData  CurrentLevelData => _levelData;

        public void Init(LevelData levelData, GameObject blockPrefab)
        {
            _levelData   = levelData;
            _blockPrefab = blockPrefab;
        }

        public List<GridColumn> GenerateGrid(LevelData levelData = null)
        {
            var data = levelData != null ? levelData : _levelData;
            if(data == null)
            {
                Debug.LogError("[LevelGenerator] LevelData atanmamis!", this);
                return new List<GridColumn>();
            }

            if(_blockPrefab == null)
            {
                Debug.LogError("[LevelGenerator] Block Prefab atanmamis!", this);
                return new List<GridColumn>();
            }

            ClearLevel();
            BuildColorLookup();

            int rows    = data.Row;
            int columns = data.Column;

            float stepX = _cellSize + _cellPadding;
            float stepY = _cellSize + _cellPadding;

            // Yatayda ortala
            float totalWidth = columns * _cellSize + (columns - 1) * _cellPadding;
            float originX    = -totalWidth / 2f + _cellSize / 2f;

            // Dikeyde SABIT taban cizgisi (Bottom row her zaman sabit Y noktasindadir)
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

            // Alttaki satirdan (rows - 1) baslayarak en ust satira (0) dogru olustur
            for(int r = rows - 1; r >= 0; r--)
            {
                int   rowIndexFromBottom = (rows - 1) - r;
                float y                  = bottomY + rowIndexFromBottom * stepY;

                for(int c = 0; c < columns; c++)
                {
                    BlockType cellType = data.GetCell(r, c);
                    if(cellType == BlockType.Empty)
                        continue;

                    float x        = originX + c * stepX;
                    var   worldPos = new Vector3(x, y, 0f);

                    var blockObj = Instantiate(_blockPrefab, worldPos, Quaternion.identity, transform);
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

            Debug.Log($"[LevelGenerator] {_spawnedBlocks.Count} GridBlock uretildi ({rows}x{columns} grid). Taban Y: {bottomY}");
            return gridColumns;
        }

        public void ClearLevel()
        {
            foreach(var block in _spawnedBlocks)
            {
                if(block != null)
                    Destroy(block);
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
                {
                    _colorLookup[entry.BlockType] = entry.Color;
                }
            }
        }

        public void ApplyBlockColor(GameObject blockObj, BlockType blockType)
        {
            if(blockObj == null) return;

            var renderer = blockObj.GetComponentInChildren<Renderer>();
            if(renderer == null)
            {
                Debug.LogWarning($"[LevelGenerator] '{blockObj.name}' icinde Renderer bulunamadi.");
                return;
            }

            if(_colorLookup == null)
                BuildColorLookup();

            if(_propBlock == null)
                _propBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_propBlock);

            if(_colorLookup != null && _colorLookup.TryGetValue(blockType, out Color color))
                _propBlock.SetColor(_baseColorId, color);
            else
                _propBlock.SetColor(_baseColorId, Color.white);

            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
