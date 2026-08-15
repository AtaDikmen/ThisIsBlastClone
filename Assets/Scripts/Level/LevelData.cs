using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Grid'in tek bir satirini temsil eder.
    /// </summary>
    [Serializable]
    public struct GridRow
    {
        [Tooltip("Sol'dan saga, bu satirdaki blok tipleri.")]
        public BlockType[] Cells;
    }

    /// <summary>
    /// ShooterBlock kuyruğunun tek bir elemanini temsil eder.
    /// </summary>
    [Serializable]
    public struct ShooterBlockEntry
    {
        [Tooltip("Bu ShooterBlock'un rengi / blok tipi.")]
        public BlockType Type;

        [Tooltip("Bu ShooterBlock'un kaç GridBlock patlatabileceği. Minimum 1.")]
        [Min(1)]
        public int BulletCount;
    }

    /// <summary>
    /// Bir levela ait tum grid verisini tutan ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData_", menuName = "ThisIsBlast/Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        [Header("Grid Boyutlari")]
        [Tooltip("Grid'in satir sayisi (dikey eksen).")]
        [Min(1)]
        public int Row = 5;

        [Tooltip("Grid'in sutun sayisi (yatay eksen).")]
        [Min(1)]
        public int Column = 5;

        [Header("Shooter Yapilandirmasi")]
        [Tooltip("Bu levelde kac ShooterBlock slotu olacak.")]
        [Min(1)]
        public int SlotCount = 5; // Case geregi varsayilan 5 slot

        [Tooltip("Kuyruktan cikacak ShooterBlock'larin listesi.")]
        public ShooterBlockEntry[] ShooterBlocks;

        [Header("Grid Verisi")]
        [Tooltip("Her eleman bir satiri temsil eder. Rows[0] en ust satir, Rows[Row-1] en alt satirdir.")]
        public GridRow[] Rows;

        public BlockType GetCell(int row, int col)
        {
            if (Rows == null || row < 0 || row >= Rows.Length)
                return BlockType.Empty;

            if (Rows[row].Cells == null || col < 0 || col >= Rows[row].Cells.Length)
                return BlockType.Empty;

            return Rows[row].Cells[col];
        }

        public Dictionary<BlockType, int> GetGridColorCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            if (Rows == null) return counts;

            for (int r = 0; r < Rows.Length; r++)
            {
                if (Rows[r].Cells == null) continue;
                for (int c = 0; c < Rows[r].Cells.Length; c++)
                {
                    var type = Rows[r].Cells[c];
                    if (type == BlockType.Empty) continue;

                    if (!counts.ContainsKey(type)) counts[type] = 0;
                    counts[type]++;
                }
            }
            return counts;
        }

        public Dictionary<BlockType, int> GetShooterBulletCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            if (ShooterBlocks == null) return counts;

            for (int i = 0; i < ShooterBlocks.Length; i++)
            {
                var entry = ShooterBlocks[i];
                if (entry.Type == BlockType.Empty) continue;

                if (!counts.ContainsKey(entry.Type)) counts[entry.Type] = 0;
                counts[entry.Type] += entry.BulletCount;
            }
            return counts;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Grid'deki blok renklerini sayip her bir renk icin 20 mermilik ShooterCannon paketleri olusturur.
        /// </summary>
        public void AutoSyncShootersFixed(int fixedBulletCount = 20)
        {
            var gridCounts  = GetGridColorCounts();
            var shooterList = new List<ShooterBlockEntry>();
            var rng         = new System.Random();

            foreach (var kvp in gridCounts)
            {
                BlockType color            = kvp.Key;
                int       totalColorBlocks = kvp.Value;

                if (totalColorBlocks <= 0) continue;

                // Grid'deki toplam rengi 20'serlik cannon paketlerine bol
                int requiredCannons = Mathf.CeilToInt((float)totalColorBlocks / fixedBulletCount);

                for (int i = 0; i < requiredCannons; i++)
                {
                    shooterList.Add(new ShooterBlockEntry
                    {
                        Type        = color,
                        BulletCount = fixedBulletCount
                    });
                }
            }

            // Fisher-Yates ile kuyrugu karistir
            for (int i = shooterList.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (shooterList[i], shooterList[k]) = (shooterList[k], shooterList[i]);
            }

            ShooterBlocks = shooterList.ToArray();
        }

        /// <summary>
        /// Grid'i kumeleme (Clustering) algoritmasi ile doldurur. 
        /// Her hucre %75 ihtimalle solundaki veya altindaki komsunun rengini alarak derli toplu adaciklar olusturur.
        /// </summary>
        public void GenerateRandomLevelClustered(BlockType[] availableColors = null, float clusterChance = 0.75f)
        {
            if (availableColors == null || availableColors.Length == 0)
            {
                availableColors = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
            }

            Rows = new GridRow[Row];
            var rng = new System.Random();

            for (int r = 0; r < Row; r++)
            {
                Rows[r].Cells = new BlockType[Column];
                for (int c = 0; c < Column; c++)
                {
                    BlockType selectedColor;

                    if (r == 0 && c == 0)
                    {
                        selectedColor = availableColors[rng.Next(0, availableColors.Length)];
                    }
                    else
                    {
                        var neighborColors = new List<BlockType>();
                        if (c > 0) neighborColors.Add(Rows[r].Cells[c - 1]); // Sol komsu
                        if (r > 0) neighborColors.Add(Rows[r - 1].Cells[c]); // Alt komsu

                        if (neighborColors.Count > 0 && rng.NextDouble() < clusterChance)
                        {
                            selectedColor = neighborColors[rng.Next(0, neighborColors.Count)];
                        }
                        else
                        {
                            selectedColor = availableColors[rng.Next(0, availableColors.Length)];
                        }
                    }

                    Rows[r].Cells[c] = selectedColor;
                }
            }

            AutoSyncShootersFixed(20);
        }

        /// <summary>
        /// Grid boyutunu gunceller. Mevcut blok verisini korur, yeni acilan hucrelere kumeli renk atar.
        /// </summary>
        public void ResizeGrid()
        {
            var old = Rows;
            Rows = new GridRow[Row];

            var colorPool = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
            var rng = new System.Random();

            for (int r = 0; r < Row; r++)
            {
                Rows[r].Cells = new BlockType[Column];
                bool isOldRowValid = old != null && r < old.Length && old[r].Cells != null;

                for (int c = 0; c < Column; c++)
                {
                    if (isOldRowValid && c < old[r].Cells.Length)
                    {
                        Rows[r].Cells[c] = old[r].Cells[c];
                    }
                    else
                    {
                        // Komsu rengi koruyarak yeni hucre olustur
                        BlockType color = colorPool[rng.Next(0, colorPool.Length)];
                        if (c > 0) color = Rows[r].Cells[c - 1];
                        else if (r > 0 && Rows[r - 1].Cells != null) color = Rows[r - 1].Cells[c];

                        Rows[r].Cells[c] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Grid icindeki 'Empty' kalmis hucreleri komsularinin renkleriyle tamamlarlar.
        /// </summary>
        public void FixEmptyCellsWithRandomColors()
        {
            if (Rows == null) return;

            var colorPool = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
            var rng = new System.Random();

            for (int r = 0; r < Rows.Length; r++)
            {
                if (Rows[r].Cells == null) continue;

                for (int c = 0; c < Rows[r].Cells.Length; c++)
                {
                    if (Rows[r].Cells[c] == BlockType.Empty)
                    {
                        BlockType color = colorPool[rng.Next(0, colorPool.Length)];
                        if (c > 0 && Rows[r].Cells[c - 1] != BlockType.Empty) color = Rows[r].Cells[c - 1];
                        else if (r > 0 && Rows[r - 1].Cells != null && Rows[r - 1].Cells[c] != BlockType.Empty) color = Rows[r - 1].Cells[c];

                        Rows[r].Cells[c] = color;
                    }
                }
            }
        }
#endif
    }
}