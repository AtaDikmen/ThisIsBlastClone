#if UNITY_EDITOR
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
        private BlockType _selectedPaintType = BlockType.Red;

        private int _genBombCount    = 1;
        private int _genArmoredCount = 1;
        private int _genRainbowCount = 1;

        private void OnEnable()
        {
            _data = (LevelData)target;
            _data.ValidateGrid();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawValidationPanel();
            EditorGUILayout.Space(6);
            DrawToolsSection();
            EditorGUILayout.Space(8);

            DrawInteractiveGridPainter();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Inspector Verileri", EditorStyles.boldLabel);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInteractiveGridPainter()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎨 Grid Boyama", EditorStyles.boldLabel);

            _selectedPaintType = (BlockType)EditorGUILayout.EnumPopup("Fırça Rengi / Özel Blok:", _selectedPaintType);
            EditorGUILayout.Space(4);

            for(int r = 0; r < _data.Row; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for(int c = 0; c < _data.Column; c++)
                {
                    BlockType currentType = _data.GetCell(r, c);
                    GUI.backgroundColor = GetColorForBlockType(currentType);

                    if(GUILayout.Button($"{currentType}", GUILayout.Width(65), GUILayout.Height(26)))
                    {
                        Undo.RecordObject(_data, "Paint Grid Cell");
                        _data.SetCell(r, c, _selectedPaintType);
                        EditorUtility.SetDirty(_data);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawValidationPanel()
        {
            var gridCounts    = _data.GetGridColorCounts();
            var shooterCounts = _data.GetShooterBulletCounts();

            int totalGridBlocks = 0;
            foreach(var kvp in gridCounts)
            {
                if(kvp.Key != BlockType.Armored && kvp.Key != BlockType.Bomb && kvp.Key != BlockType.Rainbow)
                    totalGridBlocks += kvp.Value;
            }

            int totalBullets                                      = 0;
            foreach(var val in shooterCounts.Values) totalBullets += val;

            bool isBalanced = (totalGridBlocks > 0 && totalBullets == totalGridBlocks);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Denge Durumu", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Normal Grid Blok: {totalGridBlocks}  |  Toplam Mermi: {totalBullets}");

            if(isBalanced)
            {
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f, 0.8f);
                EditorGUILayout.HelpBox("✅ Mermi ve blok sayıları %100 eşit.", MessageType.Info);
            }
            else
            {
                GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f, 0.8f);
                EditorGUILayout.HelpBox("⚠️ Mermi/Blok dengesiz! 'Shooter Eşitle' butonunu kullanın.", MessageType.Warning);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠️ Rastgele Level Üretici", EditorStyles.boldLabel);

            _genBombCount    = EditorGUILayout.IntSlider("💣 Bomba Adedi:", _genBombCount, 0, 10);
            _genArmoredCount = EditorGUILayout.IntSlider("🛡️ Zırhlı (Armored) Adedi:", _genArmoredCount, 0, 10);
            _genRainbowCount = EditorGUILayout.IntSlider("🌈 Rainbow Adedi:", _genRainbowCount, 0, 10);

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.9f, 0.6f, 1f);
            if(GUILayout.Button("🎲 Ayarlara Göre Level Üret", GUILayout.Height(28)))
            {
                Undo.RecordObject(_data, "Generate Custom Random Level");
                var pool = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };

                _data.GenerateRandomLevelClustered(
                    availableColors: pool,
                    clusterChance: 0.75f,
                    bombCount: _genBombCount,
                    armoredCount: _genArmoredCount,
                    rainbowCount: _genRainbowCount
                );

                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if(GUILayout.Button("⚡ Shooter Eşitle", GUILayout.Height(24)))
            {
                Undo.RecordObject(_data, "Auto Sync Shooters");
                _data.AutoSyncShootersDynamic();
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private Color GetColorForBlockType(BlockType type) => type switch
                                                              {
                                                                  BlockType.Red     => new Color(0.9f, 0.35f, 0.35f),
                                                                  BlockType.Blue    => new Color(0.35f, 0.55f, 0.95f),
                                                                  BlockType.Green   => new Color(0.35f, 0.85f, 0.45f),
                                                                  BlockType.Yellow  => new Color(0.95f, 0.85f, 0.25f),
                                                                  BlockType.Purple  => new Color(0.7f, 0.35f, 0.85f),
                                                                  BlockType.Armored => new Color(0.3f, 0.3f, 0.35f),
                                                                  BlockType.Bomb    => new Color(0.85f, 0.2f, 0.1f),
                                                                  BlockType.Rainbow => new Color(0.95f, 0.4f, 0.8f),
                                                                  _                 => Color.gray
                                                              };
    }
}
#endif
