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

            _selectedPaintType = (BlockType)EditorGUILayout.EnumPopup("Fırça Rengi:", _selectedPaintType);
            EditorGUILayout.Space(4);

            for(int r = 0; r < _data.Row; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for(int c = 0; c < _data.Column; c++)
                {
                    BlockType currentType = _data.GetCell(r, c);
                    GUI.backgroundColor = GetColorForBlockType(currentType);

                    if(GUILayout.Button($"{currentType}", GUILayout.Width(60), GUILayout.Height(26)))
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
                if(kvp.Key != BlockType.Obstacle_Iron) totalGridBlocks += kvp.Value;
            }

            int totalBullets                                      = 0;
            foreach(var val in shooterCounts.Values) totalBullets += val;

            bool isBalanced = (totalGridBlocks > 0 && totalBullets == totalGridBlocks);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Denge Durumu", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Grid Blok: {totalGridBlocks}  |  Toplam Mermi: {totalBullets}");

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
            EditorGUILayout.LabelField("🛠️ Hızlı Araçlar", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.9f, 0.6f, 1f);
            if(GUILayout.Button("🎲 Rastgele Level Üret", GUILayout.Height(26)))
            {
                Undo.RecordObject(_data, "Generate Clustered Random Level");
                var pool = new[] { BlockType.Red, BlockType.Blue, BlockType.Green, BlockType.Yellow, BlockType.Purple };
                _data.GenerateRandomLevelClustered(pool, clusterChance: 0.75f);
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if(GUILayout.Button("⚡ Shooter Eşitle (10/20 Mermi)", GUILayout.Height(26)))
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
                                                                  BlockType.Red           => new Color(0.9f, 0.35f, 0.35f),
                                                                  BlockType.Blue          => new Color(0.35f, 0.55f, 0.95f),
                                                                  BlockType.Green         => new Color(0.35f, 0.85f, 0.45f),
                                                                  BlockType.Yellow        => new Color(0.95f, 0.85f, 0.25f),
                                                                  BlockType.Purple        => new Color(0.7f, 0.35f, 0.85f),
                                                                  BlockType.Obstacle_Iron => new Color(0.45f, 0.45f, 0.5f),
                                                                  _                       => Color.gray
                                                              };
    }
}
#endif
