using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Level
{
    [Serializable]
    public struct ShooterBlockEntry
    {
        public           BlockType Type;
        [Min(10)] public int       BulletCount;
    }

    [CreateAssetMenu(fileName = "LevelData_", menuName = "SO/Level Data", order = 0)]
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
                    if(type == BlockType.Empty || type == BlockType.Bomb || type == BlockType.Rainbow)
                        continue;

                    if(!counts.ContainsKey(type)) counts[type] = 0;

                    int hp = (type == BlockType.Armored) ? 20 : 1;
                    counts[type] += hp;
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

        public void AutoSyncShootersOverSupplied(float multiplier = 1.5f)
        {
            var gridHpCounts = GetGridColorCounts();
            var shooterList  = new List<ShooterBlockEntry>();
            var rng          = new System.Random();

            var activeBaseColors = new List<BlockType>();
            foreach(var kvp in gridHpCounts)
            {
                if(IsBaseColor(kvp.Key) && kvp.Value > 0)
                    activeBaseColors.Add(kvp.Key);
            }

            if(activeBaseColors.Count == 0)
            {
                activeBaseColors.AddRange(new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple });
            }

            foreach(var kvp in gridHpCounts)
            {
                var type   = kvp.Key;
                int baseHp = kvp.Value;

                if(baseHp <= 0 || !IsBaseColor(type)) continue;

                int targetBulletCount = Mathf.CeilToInt(baseHp * multiplier);

                while(targetBulletCount > 0)
                {
                    int bulletCount = (targetBulletCount >= 20 && rng.Next(0, 2) == 0) ? 20 : 10;

                    shooterList.Add(new ShooterBlockEntry
                                    {
                                        Type        = type,
                                        BulletCount = bulletCount
                                    });

                    targetBulletCount -= bulletCount;
                }
            }

            if(gridHpCounts.TryGetValue(BlockType.Armored, out int armoredHp) && armoredHp > 0)
            {
                int extraArmoredBullets = Mathf.CeilToInt(armoredHp * multiplier);

                while(extraArmoredBullets > 0)
                {
                    var randomBaseColor = activeBaseColors[rng.Next(0, activeBaseColors.Count)];
                    int bulletCount     = (extraArmoredBullets >= 20 && rng.Next(0, 2) == 0) ? 20 : 10;

                    shooterList.Add(new ShooterBlockEntry
                                    {
                                        Type        = randomBaseColor,
                                        BulletCount = bulletCount
                                    });

                    extraArmoredBullets -= bulletCount;
                }
            }

            for(int i = shooterList.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (shooterList[i], shooterList[k]) = (shooterList[k], shooterList[i]);
            }

            ShooterBlocks = shooterList.ToArray();
        }

        private bool IsBaseColor(BlockType type)
        {
            return type == BlockType.Red ||
                   type == BlockType.Blue ||
                   type == BlockType.Green ||
                   type == BlockType.Yellow ||
                   type == BlockType.Purple;
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
                for(int c = 0; c < Column; c++)
                    allPositions.Add((r, c));

            for(int i = allPositions.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (allPositions[i], allPositions[k]) = (allPositions[k], allPositions[i]);
            }

            int currentIdx = 0;

            for(int i = 0; i < bombCount && currentIdx < allPositions.Count; i++, currentIdx++)
                SetCell(allPositions[currentIdx].r, allPositions[currentIdx].c, BlockType.Bomb);

            for(int i = 0; i < armoredCount && currentIdx < allPositions.Count; i++, currentIdx++)
                SetCell(allPositions[currentIdx].r, allPositions[currentIdx].c, BlockType.Armored);

            for(int i = 0; i < rainbowCount && currentIdx < allPositions.Count; i++, currentIdx++)
                SetCell(allPositions[currentIdx].r, allPositions[currentIdx].c, BlockType.Rainbow);

            AutoSyncShootersOverSupplied();
        }
#endif
    }
}
