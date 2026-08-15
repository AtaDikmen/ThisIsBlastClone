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

        private bool _showRandomSettings = false;
        private int  _colorCount         = 4;
        private int  _minBullets         = 2;
        private int  _maxBullets         = 5;

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
            var gridCounts    = _data.GetGridColorCounts();
            var shooterCounts = _data.GetShooterBulletCounts();

            int totalGridBlocks                                   = 0;
            foreach(var val in gridCounts.Values) totalGridBlocks += val;

            int totalBullets                                      = 0;
            foreach(var val in shooterCounts.Values) totalBullets += val;

            // Birebir eslesme kontrolu
            bool isAllColorsMatch = true;
            var  allTypes         = new HashSet<BlockType>(gridCounts.Keys);
            foreach(var k in shooterCounts.Keys) allTypes.Add(k);

            var mismatchList = new List<string>();

            foreach(var type in allTypes)
            {
                int gridAmount    = gridCounts.GetValueOrDefault(type, 0);
                int shooterAmount = shooterCounts.GetValueOrDefault(type, 0);

                if(gridAmount != shooterAmount)
                {
                    isAllColorsMatch = false;
                    int    diff = shooterAmount - gridAmount;
                    string sign = diff > 0 ? $"+{diff}" : $"{diff}";
                    mismatchList.Add($"{type}: Grid={gridAmount}, Mermi={shooterAmount} ({sign})");
                }
            }

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("📊 Canlı Denge & Doğrulama", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Toplam Grid Küpü: {totalGridBlocks}  |  Toplam Shooter Mermisi: {totalBullets}");

            if(totalGridBlocks == totalBullets && isAllColorsMatch)
            {
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f, 0.8f);
                EditorGUILayout.HelpBox(
                    "✅ MÜKEMMEL DENGEDE!\nGrid'deki tüm küp sayıları ile Shooter mermileri %100 birebir uyuşuyor. Level sorunsuz bitirilebilir.",
                    MessageType.Info);
            }
            else
            {
                GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f, 0.8f);
                string details = string.Join("\n • ", mismatchList);
                EditorGUILayout.HelpBox(
                    $"⚠️ UYUMSUZLUK TESPİT EDİLDİ!\nFarklar:\n • {details}\n(Aşağıdaki '⚡ Shooter Kuyruğunu Senkronize Et' butonunu kullanarak otomatik düzeltebilirsiniz)",
                    MessageType.Warning);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠️ Seviye Tasarım Araçları", EditorStyles.boldLabel);

            // 1. Grid Boyutlandir
            if(GUILayout.Button("📐 Grid Boyutunu Uygula (Resize Grid)", GUILayout.Height(24)))
            {
                Undo.RecordObject(_data, "Resize Grid");
                _data.ResizeGrid();
                EditorUtility.SetDirty(_data);
            }

            // 2. Senkronize Et
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if(GUILayout.Button("⚡ Shooter Kuyruğunu Grid'den Senkronize Et", GUILayout.Height(28)))
            {
                Undo.RecordObject(_data, "Auto Sync Shooters");
                _data.AutoSyncShootersFromGrid(_minBullets, _maxBullets);
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            // 3. Rastgele Level Uretimi
            _showRandomSettings = EditorGUILayout.Foldout(_showRandomSettings, "🎲 Rastgele Level Üretici (Ayarlar)", true);
            if(_showRandomSettings)
            {
                EditorGUI.indentLevel++;
                _colorCount = EditorGUILayout.IntSlider("Kullanılacak Renk Sayısı", _colorCount, 2, 5);
                _minBullets = EditorGUILayout.IntSlider("Min Mermi / Küp", _minBullets, 1, 4);
                _maxBullets = EditorGUILayout.IntSlider("Max Mermi / Küp", _maxBullets, _minBullets, 8);
                EditorGUI.indentLevel--;

                GUI.backgroundColor = new Color(0.9f, 0.6f, 1f);
                if(GUILayout.Button("🎲 Rastgele Seviye Oluştur (Grid + Shooter)", GUILayout.Height(30)))
                {
                    Undo.RecordObject(_data, "Generate Random Level");

                    var pool                                             = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
                    var selected                                         = new BlockType[Mathf.Clamp(_colorCount, 2, pool.Length)];
                    for(int i = 0; i < selected.Length; i++) selected[i] = pool[i];

                    _data.GenerateRandomLevel(selected, _minBullets, _maxBullets);
                    EditorUtility.SetDirty(_data);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
