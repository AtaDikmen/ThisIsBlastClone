#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Level
{
    [Serializable]
    public struct ShooterBlockEntry
    {
        public          BlockType Type;
        [Min(1)] public int       BulletCount;
    }

    [CreateAssetMenu(fileName = "LevelData_", menuName = "ThisIsBlast/Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        [Header("Grid Boyutları")]
        [Min(1)] public int Row = 8;
        [Min(1)] public int Column = 5;

        [Header("Shooter Ayarları")]
        [Min(1)] public int SlotCount = 5;
        public ShooterBlockEntry[] ShooterBlocks;

        [Header("Grid Verisi")]
        [SerializeField] private BlockType[] gridData;

        public BlockType GetCell(int r, int c)
        {
            ValidateGrid();
            if(r < 0 || r >= Row || c < 0 || c >= Column) return BlockType.Empty;
            return gridData[r * Column + c];
        }

        public void SetCell(int r, int c, BlockType type)
        {
            ValidateGrid();
            if(r < 0 || r >= Row || c < 0 || c >= Column) return;
            gridData[r * Column + c] = type;
        }

        public void ValidateGrid()
        {
            int requiredLength = Row * Column;
            if(gridData == null || gridData.Length != requiredLength)
            {
                Array.Resize(ref gridData, requiredLength);
            }
        }

        public Dictionary<BlockType, int> GetGridColorCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            ValidateGrid();

            for(int r = 0; r < Row; r++)
            {
                for(int c = 0; c < Column; c++)
                {
                    var type = GetCell(r, c);
                    if(type == BlockType.Empty) continue;

                    if(!counts.ContainsKey(type)) counts[type] = 0;
                    counts[type]++;
                }
            }
            return counts;
        }

        public Dictionary<BlockType, int> GetShooterBulletCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            if(ShooterBlocks == null) return counts;

            for(int i = 0; i < ShooterBlocks.Length; i++)
            {
                var entry = ShooterBlocks[i];
                if(entry.Type == BlockType.Empty) continue;

                counts.TryAdd(entry.Type, 0);
                counts[entry.Type] += entry.BulletCount;
            }
            return counts;
        }

        public void AutoSyncShootersDynamic()
        {
            NormalizeGridCountsToMultiplesOfTen();

            var gridCounts  = GetGridColorCounts();
            var shooterList = new List<ShooterBlockEntry>();
            var rng         = new System.Random();

            foreach(var kvp in gridCounts)
            {
                BlockType color            = kvp.Key;
                int       totalColorBlocks = kvp.Value;

                // Özel bloklar ve engeller shooter eşitlemesine dahil edilmez
                if(color == BlockType.Armored || color == BlockType.Bomb || color == BlockType.Rainbow || totalColorBlocks <= 0)
                    continue;

                while(totalColorBlocks > 0)
                {
                    int bulletCount = (totalColorBlocks >= 30) ? ((rng.Next(0, 2) == 0) ? 10 : 20) : ((totalColorBlocks == 20) ? 20 : 10);

                    shooterList.Add(new ShooterBlockEntry
                                    {
                                        Type        = color,
                                        BulletCount = bulletCount
                                    });

                    totalColorBlocks -= bulletCount;
                }
            }

            for(int i = shooterList.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (shooterList[i], shooterList[k]) = (shooterList[k], shooterList[i]);
            }

            ShooterBlocks = shooterList.ToArray();
        }

        private void NormalizeGridCountsToMultiplesOfTen()
        {
            var counts = GetGridColorCounts();

            foreach(var kvp in counts)
            {
                BlockType color = kvp.Key;
                int       count = kvp.Value;

                if(color == BlockType.Armored || color == BlockType.Bomb || color == BlockType.Rainbow || count == 0)
                    continue;

                int remainder = count % 10;
                if(remainder == 0) continue;

                if(remainder >= 5)
                    AddBlocksToGrid(color, 10 - remainder);
                else
                    RemoveBlocksFromGrid(color, remainder);
            }
        }

        private void AddBlocksToGrid(BlockType color, int amount)
        {
            int added = 0;
            for(int i = 0; i < gridData.Length && added < amount; i++)
            {
                if(gridData[i] == BlockType.Empty)
                {
                    gridData[i] = color;
                    added++;
                }
            }
        }

        private void RemoveBlocksFromGrid(BlockType color, int amount)
        {
            int removed = 0;

            for(int r = 0; r < Row && removed < amount; r++)
            {
                for(int c = 0; c < Column && removed < amount; c++)
                {
                    if(GetCell(r, c) == color)
                    {
                        SetCell(r, c, BlockType.Empty);
                        removed++;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void GenerateRandomLevelClustered(BlockType[] availableColors = null, float clusterChance = 0.75f, int bombCount = 0, int armoredCount = 0, int rainbowCount = 0)
        {
            if(availableColors == null || availableColors.Length == 0)
            {
                availableColors = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
            }

            ValidateGrid();
            var rng = new System.Random();

            for(int r = 0; r < Row; r++)
            {
                for(int c = 0; c < Column; c++)
                {
                    BlockType selectedColor;

                    if(r == 0 && c == 0)
                    {
                        selectedColor = availableColors[rng.Next(0, availableColors.Length)];
                    }
                    else
                    {
                        var neighborColors = new List<BlockType>();
                        if(c > 0) neighborColors.Add(GetCell(r, c - 1));
                        if(r > 0) neighborColors.Add(GetCell(r - 1, c));

                        if(neighborColors.Count > 0 && rng.NextDouble() < clusterChance)
                            selectedColor = neighborColors[rng.Next(0, neighborColors.Count)];
                        else
                            selectedColor = availableColors[rng.Next(0, availableColors.Length)];
                    }

                    SetCell(r, c, selectedColor);
                }
            }

            var allPositions = new List<(int r, int c)>();
            for(int r = 0; r < Row; r++)
            {
                for(int c = 0; c < Column; c++)
                {
                    allPositions.Add((r, c));
                }
            }

            for(int i = allPositions.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (allPositions[i], allPositions[k]) = (allPositions[k], allPositions[i]);
            }

            int currentIdx = 0;

            for(int i = 0; i < bombCount && currentIdx < allPositions.Count; i++, currentIdx++)
            {
                var pos = allPositions[currentIdx];
                SetCell(pos.r, pos.c, BlockType.Bomb);
            }

            for(int i = 0; i < armoredCount && currentIdx < allPositions.Count; i++, currentIdx++)
            {
                var pos = allPositions[currentIdx];
                SetCell(pos.r, pos.c, BlockType.Armored);
            }

            for(int i = 0; i < rainbowCount && currentIdx < allPositions.Count; i++, currentIdx++)
            {
                var pos = allPositions[currentIdx];
                SetCell(pos.r, pos.c, BlockType.Rainbow);
            }

            AutoSyncShootersDynamic();
        }
#endif
    }
}
#endif
