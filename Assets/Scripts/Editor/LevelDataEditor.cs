using System.Collections.Generic;
using Data;
using Level;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private LevelData _data;
        private const int FIXED_BULLET_COUNT = 20;

        private void OnEnable()
        {
            _data = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawValidationPanel();
            EditorGUILayout.Space(8);
            DrawToolsSection();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Level Verileri", EditorStyles.boldLabel);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidationPanel()
        {
            var gridCounts = _data.GetGridColorCounts();
            var shooterCounts = _data.GetShooterBulletCounts();

            int totalGridBlocks = 0;
            foreach (var val in gridCounts.Values) totalGridBlocks += val;

            int totalBullets = 0;
            foreach (var val in shooterCounts.Values) totalBullets += val;

            bool isBalanced = (totalGridBlocks > 0 && totalBullets >= totalGridBlocks);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Canlı Denge & Doğrulama (20 Mermi Modu)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Toplam Grid Blok: {totalGridBlocks}  |  Toplam Cannon Mermisi: {totalBullets}");

            if (isBalanced)
            {
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f, 0.8f);
                EditorGUILayout.HelpBox($"✅ YETERLİ CANNON VAR!\nKuyrukta toplam {totalBullets} mermi var, grid'deki {totalGridBlocks} bloğu temizlemek için yeterli.", MessageType.Info);
            }
            else
            {
                GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f, 0.8f);
                EditorGUILayout.HelpBox($"⚠️ CANNON/MERMİ EKSİK!\nGrid'de {totalGridBlocks} blok var. Aşağıdaki senkronizasyon butonunu kullanarak 20'şerli Cannon sırasını güncelleyin.", MessageType.Warning);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠️ Seviye Tasarım Araçları", EditorStyles.boldLabel);

            // 1. Grid Boyutlandır
            if (GUILayout.Button("📐 Grid Boyutunu Uygula (Yeni Hücreleri Renklendir)", GUILayout.Height(26)))
            {
                Undo.RecordObject(_data, "Resize Grid With Colors");
                _data.ResizeGrid();
                EditorUtility.SetDirty(_data);
            }

            // 2. Boşlukları Doldur
            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.3f);
            if (GUILayout.Button("🎨 Boş Hücreleri Tamamla (Fix Empty)", GUILayout.Height(24)))
            {
                Undo.RecordObject(_data, "Fix Empty Cells");
                _data.FixEmptyCellsWithRandomColors();
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // 3. Organik Kümeli Seviye Üretici
            GUI.backgroundColor = new Color(0.9f, 0.6f, 1f);
            if (GUILayout.Button("🎲 Kümeli (Cluster) Rastgele Seviye Oluştur", GUILayout.Height(30)))
            {
                Undo.RecordObject(_data, "Generate Clustered Random Level");

                var pool = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
                _data.GenerateRandomLevelClustered(pool, clusterChance: 0.75f);

                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // 4. Cannon Kuyruğunu Senkronize Et
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("⚡ 20 Mermili Cannon Kuyruğunu Grid'den Oluştur", GUILayout.Height(30)))
            {
                Undo.RecordObject(_data, "Auto Sync 20-Bullet Shooters");
                _data.AutoSyncShootersFixed(FIXED_BULLET_COUNT);
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }
    }
}