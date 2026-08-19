using System;
using System.Collections.Generic;
using Audio;
using Block;
using Data;
using UnityEngine;
using VContainer;
using VFX;

namespace Level
{
    [Serializable]
    public struct BlockPrefabEntry
    {
        public BlockType  Type;
        public GameObject Prefab;
    }

    [Serializable]
    public struct BlockColorEntry
    {
        public BlockType BlockType;
        public Color     Color;
    }

    public class LevelGenerator : MonoBehaviour
    {
        [Header("Default Prefab")]
        [SerializeField] private GameObject _defaultGridBlockPrefab;

        [Header("Special Block Prefabs")]
        [SerializeField] private BlockPrefabEntry[] _specialBlockPrefabs;

        [Header("Grid Layout Settings")]
        [SerializeField] private float _cellPadding = 0.08f;
        [SerializeField] private float _cellSize    = 0.25f;
        [SerializeField] private float _gridBottomY = -0.4f;

        [Header("Color Palette")]
        [SerializeField] private BlockColorEntry[] _colorPalette;

        private Dictionary<BlockType, GameObject> _prefabLookup;
        private Dictionary<BlockType, Color>      _colorLookup;
        private MaterialPropertyBlock             _propBlock;

        private readonly static int ColorId     = Shader.PropertyToID("_Color");
        private readonly static int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();

        public GameObject DefaultGridBlockPrefab => _defaultGridBlockPrefab;
        public LevelData  CurrentLevelData       { get; private set; }

        private IAudioService _audioService;
        private IVFXService   _vfxService;

        [Inject]
        public void Construct(IAudioService audioService, IVFXService vfxService)
        {
            _audioService = audioService;
            _vfxService   = vfxService;
        }

        private void BuildPrefabLookup()
        {
            _prefabLookup = new Dictionary<BlockType, GameObject>();
            if(_specialBlockPrefabs == null) return;

            foreach(var entry in _specialBlockPrefabs)
            {
                if(entry.Prefab != null && !_prefabLookup.ContainsKey(entry.Type))
                {
                    _prefabLookup.Add(entry.Type, entry.Prefab);
                }
            }
        }

        public List<GridColumn> GenerateGrid(LevelData levelData)
        {
            CurrentLevelData = levelData;

            if(CurrentLevelData == null || _defaultGridBlockPrefab == null)
            {
                Debug.LogError("[LevelGenerator] LevelData veya Default GridBlockPrefab atanmamış!", this);
                return new List<GridColumn>();
            }

            ClearLevel();
            BuildColorLookup();
            BuildPrefabLookup();

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

                    var prefabToInstantiate = _defaultGridBlockPrefab;
                    if(_prefabLookup != null && _prefabLookup.TryGetValue(cellType, out var specialPrefab))
                    {
                        if(specialPrefab != null)
                            prefabToInstantiate = specialPrefab;
                    }

                    var blockObj = Instantiate(prefabToInstantiate, worldPos, Quaternion.identity, transform);
                    blockObj.name = $"GridBlock_{cellType}_{r}_{c}";

                    int       initialHealth   = 1;
                    BlockType actualColorType = cellType;

                    if(cellType == BlockType.Armored)
                    {
                        initialHealth = 20;

                        BlockType[] baseColors = { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
                        actualColorType = baseColors[UnityEngine.Random.Range(0, baseColors.Length)];

                        ApplyBlockColor(blockObj, actualColorType);
                    }
                    else if(cellType == BlockType.Rainbow)
                    {
                        // Rainbow kendi animasyonlu rengini yönetir
                    }
                    else if(cellType != BlockType.Bomb)
                    {
                        ApplyBlockColor(blockObj, actualColorType);
                    }

                    var gridBlock = blockObj.GetComponent<GridBlock>();
                    if(gridBlock == null)
                        gridBlock = blockObj.AddComponent<GridBlock>();

                    gridBlock.Setup(actualColorType, c, r, initialHealth, _audioService, _vfxService);

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
                               [BlockType.Red]    = new Color(0.90f, 0.20f, 0.20f),
                               [BlockType.Blue]   = new Color(0.20f, 0.45f, 0.90f),
                               [BlockType.Green]  = new Color(0.20f, 0.75f, 0.30f),
                               [BlockType.Yellow] = new Color(0.98f, 0.82f, 0.10f),
                               [BlockType.Purple] = new Color(0.60f, 0.20f, 0.85f)
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
            _propBlock ??= new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_propBlock);

            Color targetColor = Color.white;
            if(_colorLookup != null && _colorLookup.TryGetValue(blockType, out Color foundColor))
            {
                targetColor = foundColor;
            }

            _propBlock.SetColor(ColorId, targetColor);
            _propBlock.SetColor(BaseColorId, targetColor);

            renderer.SetPropertyBlock(_propBlock);
        }

        public int GetActiveBlockCount()
        {
            return _spawnedBlocks.Count;
        }
    }
}
