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
        public int SlotCount = 3;

        [Tooltip(
            "Kuyruktan cikacak ShooterBlock'larin listesi. " +
            "Sira: ilk eleman ilk cikar. " +
            "Toplam mermi sayisi toplam GridBlock sayisina esit olmalidir.")]
        public ShooterBlockEntry[] ShooterBlocks;

        [Header("Grid Verisi")]
        [Tooltip(
            "Her eleman bir satiri temsil eder. " +
            "Rows[0] en ust satir, Rows[Row-1] en alt satirdir.")]
        public GridRow[] Rows;

        public BlockType GetCell(int row, int col)
        {
            if(Rows == null || row < 0 || row >= Rows.Length)
                return BlockType.Empty;

            if(Rows[row].Cells == null || col < 0 || col >= Rows[row].Cells.Length)
                return BlockType.Empty;

            return Rows[row].Cells[col];
        }

        /// <summary>
        /// Grid'deki her BlockType icin toplam hucre sayisini hesaplar.
        /// </summary>
        public Dictionary<BlockType, int> GetGridColorCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            if(Rows == null) return counts;

            for(int r = 0; r < Rows.Length; r++)
            {
                if(Rows[r].Cells == null) continue;
                for(int c = 0; c < Rows[r].Cells.Length; c++)
                {
                    var type = Rows[r].Cells[c];
                    if(type == BlockType.Empty) continue;

                    if(!counts.ContainsKey(type)) counts[type] = 0;
                    counts[type]++;
                }
            }
            return counts;
        }

        /// <summary>
        /// Shooter kuyrugundaki her BlockType icin toplam mermi sayisini hesaplar.
        /// </summary>
        public Dictionary<BlockType, int> GetShooterBulletCounts()
        {
            var counts = new Dictionary<BlockType, int>();
            if(ShooterBlocks == null) return counts;

            for(int i = 0; i < ShooterBlocks.Length; i++)
            {
                var entry = ShooterBlocks[i];
                if(entry.Type == BlockType.Empty) continue;

                if(!counts.ContainsKey(entry.Type)) counts[entry.Type] = 0;
                counts[entry.Type] += entry.BulletCount;
            }
            return counts;
        }

#if UNITY_EDITOR

        public void ResizeGrid()
        {
            var old = Rows;
            Rows = new GridRow[Row];

            for(int r = 0; r < Row; r++)
            {
                Rows[r].Cells = new BlockType[Column];

                if(old != null && r < old.Length && old[r].Cells != null)
                {
                    for(int c = 0; c < Column && c < old[r].Cells.Length; c++)
                        Rows[r].Cells[c] = old[r].Cells[c];
                }
            }
        }

        /// <summary>
        /// Grid'i secilen renklerle rastgele doldurur ve mermi sayisiyla
        /// %100 birebir uyuşan Shooter kuyrugunu otomatik olusturur.
        /// </summary>
        public void GenerateRandomLevel(
            BlockType[] availableColors,
            int         minBulletsPerShooter = 2,
            int         maxBulletsPerShooter = 4)
        {
            if(availableColors == null || availableColors.Length == 0)
            {
                availableColors = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow };
            }

            // 1. Grid'i boyutlandir ve rastgele doldur
            Rows = new GridRow[Row];
            var rng = new System.Random();

            for(int r = 0; r < Row; r++)
            {
                Rows[r].Cells = new BlockType[Column];
                for(int c = 0; c < Column; c++)
                {
                    int colorIdx = rng.Next(0, availableColors.Length);
                    Rows[r].Cells[c] = availableColors[colorIdx];
                }
            }

            // 2. Grid'deki renkleri say ve Shooter kuyrugunu olustur
            AutoSyncShootersFromGrid(minBulletsPerShooter, maxBulletsPerShooter);
        }

        /// <summary>
        /// Mevcut Grid icerigini okur ve toplam renk sayilarina birebir esit mermiye
        /// sahip Shooter kuyrugunu olusturur.
        /// </summary>
        public void AutoSyncShootersFromGrid(int minBullets = 2, int maxBullets = 4)
        {
            var gridCounts  = GetGridColorCounts();
            var shooterList = new List<ShooterBlockEntry>();
            var rng         = new System.Random();

            foreach(var kvp in gridCounts)
            {
                BlockType color            = kvp.Key;
                int       remainingBullets = kvp.Value;

                while(remainingBullets > 0)
                {
                    int bullets = rng.Next(minBullets, maxBullets + 1);
                    if(bullets > remainingBullets)
                        bullets = remainingBullets;

                    shooterList.Add(new ShooterBlockEntry
                                    {
                                        Type        = color,
                                        BulletCount = bullets
                                    });

                    remainingBullets -= bullets;
                }
            }

            // Kuyrugu karistir (Shuffle)
            for(int i = shooterList.Count - 1; i > 0; i--)
            {
                int k    = rng.Next(i + 1);
                (shooterList[i], shooterList[k]) = (shooterList[k], shooterList[i]);
            }

            ShooterBlocks = shooterList.ToArray();
        }
#endif
    }
}
