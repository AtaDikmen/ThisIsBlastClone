#if UNITY_EDITOR
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
        private BlockType _selectedPaintType = BlockType.Red;

        private int _genBombCount    = 1;
        private int _genArmoredCount = 1;
        private int _genRainbowCount = 1;

        private bool    _showShooterVisualizer = true;
        private int     _newShooterBulletCount = 10;
        private Vector2 _shooterScrollPosition = Vector2.zero;

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

            DrawShooterQueueVisualizer();
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Inspector Default Verileri", EditorStyles.boldLabel);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawShooterQueueVisualizer()
        {
            EditorGUILayout.BeginVertical("box");

            _showShooterVisualizer = EditorGUILayout.Foldout(_showShooterVisualizer, "🎯 Shooter Queue Matrisi (Sıra & Görselleştirici)", true, EditorStyles.foldoutHeader);

            if(_showShooterVisualizer)
            {
                EditorGUILayout.Space(4);

                if(_data.ShooterBlocks == null || _data.ShooterBlocks.Length == 0)
                {
                    EditorGUILayout.HelpBox("Kuyrukta henüz Shooter yok.", MessageType.Info);
                }
                else
                {
                    int laneCount     = Mathf.Max(1, _data.SlotCount);
                    int totalShooters = _data.ShooterBlocks.Length;
                    int maxRows       = Mathf.CeilToInt((float)totalShooters / laneCount);

                    EditorGUILayout.LabelField($"Toplam Shooter: {totalShooters} | Slot (Sütun) Sayısı: {laneCount} | Satır Sayısı: {maxRows}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(4);

                    _shooterScrollPosition = EditorGUILayout.BeginScrollView(_shooterScrollPosition, GUILayout.MinHeight(180), GUILayout.MaxHeight(320));
                    EditorGUILayout.BeginHorizontal();

                    for(int lane = 0; lane < laneCount; lane++)
                    {
                        EditorGUILayout.BeginVertical("helpBox", GUILayout.Width(62));
                        EditorGUILayout.LabelField($"Lane {lane + 1}", EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.Space(2);

                        for(int i = lane; i < _data.ShooterBlocks.Length; i += laneCount)
                            DrawShooterCard(i);

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space(6);
                DrawAddShooterBar();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShooterCard(int index)
        {
            var   entry      = _data.ShooterBlocks[index];
            Color blockColor = GetColorForBlockType(entry.Type);

            GUI.backgroundColor = blockColor;

            EditorGUILayout.BeginVertical("box");

            var cardStyle = new GUIStyle(GUI.skin.button)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontStyle = FontStyle.Bold,
                                fontSize  = 10,
                                normal    = { textColor = GetContrastingColor(blockColor) }
                            };

            if(GUILayout.Button($"{entry.Type}\n[{entry.BulletCount}]", cardStyle, GUILayout.Height(32)))
            {
                Undo.RecordObject(_data, "Change Shooter Type");
                _data.ShooterBlocks[index].Type = _selectedPaintType;
                EditorUtility.SetDirty(_data);
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Button("-", GUILayout.Width(16), GUILayout.Height(15)))
            {
                Undo.RecordObject(_data, "Decrease Ammo");
                _data.ShooterBlocks[index].BulletCount = Mathf.Max(5, entry.BulletCount - 5);
                EditorUtility.SetDirty(_data);
            }
            if(GUILayout.Button("+", GUILayout.Width(16), GUILayout.Height(15)))
            {
                Undo.RecordObject(_data, "Increase Ammo");
                _data.ShooterBlocks[index].BulletCount += 5;
                EditorUtility.SetDirty(_data);
            }
            if(GUILayout.Button("×", GUILayout.Width(16), GUILayout.Height(15)))
            {
                Undo.RecordObject(_data, "Remove Shooter");
                RemoveShooterAtIndex(index);
                EditorUtility.SetDirty(_data);
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawAddShooterBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hızlı Ekle:", GUILayout.Width(65));

            _selectedPaintType     = (BlockType)EditorGUILayout.EnumPopup(_selectedPaintType, GUILayout.Width(80));
            _newShooterBulletCount = EditorGUILayout.IntField(_newShooterBulletCount, GUILayout.Width(35));

            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
            if(GUILayout.Button("+ Ekle", GUILayout.Height(18)))
            {
                Undo.RecordObject(_data, "Add Manual Shooter");
                AddShooter(_selectedPaintType, Mathf.Max(1, _newShooterBulletCount));
                EditorUtility.SetDirty(_data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void AddShooter(BlockType type, int bulletCount)
        {
            var list = new List<ShooterBlockEntry>(_data.ShooterBlocks ?? new ShooterBlockEntry[0]);
            list.Add(new ShooterBlockEntry { Type = type, BulletCount = bulletCount });
            _data.ShooterBlocks = list.ToArray();
        }

        private void RemoveShooterAtIndex(int index)
        {
            var list = new List<ShooterBlockEntry>(_data.ShooterBlocks);
            if(index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
                _data.ShooterBlocks = list.ToArray();
            }
        }

        private Color GetContrastingColor(Color color)
        {
            float luminance = (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b);
            return luminance > 0.6f ? Color.black : Color.white;
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

                    if(GUILayout.Button($"{currentType}", GUILayout.Width(65), GUILayout.Height(24)))
                    {
                        Undo.RecordObject(_data, "Paint Grid Cell");
                        _data.SetCell(r, c, _selectedPaintType);
                        _data.AutoSyncShootersOverSupplied(1.5f);
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

            int totalGridHp                            = 0;
            foreach(var kvp in gridCounts) totalGridHp += kvp.Value;

            int totalBullets                                      = 0;
            foreach(var val in shooterCounts.Values) totalBullets += val;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Denge Durumu", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Grid HP İhtiyacı: {totalGridHp}  |  Toplam Shooter Mermisi: {totalBullets}");

            if(totalBullets >= totalGridHp && totalGridHp > 0)
            {
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f, 0.8f);
                EditorGUILayout.HelpBox($"✅ Otomatik Denge Aktif! (Mermi Oranı: %{(int)((float)totalBullets / totalGridHp * 100)})", MessageType.Info);
            }
            else
            {
                GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f, 0.8f);
                EditorGUILayout.HelpBox("⚠️ Mermi yetersiz veya grid boş.", MessageType.Warning);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠️ Otomatik Level Üretici", EditorStyles.boldLabel);

            _genBombCount    = EditorGUILayout.IntSlider("💣 Bomba Adedi:", _genBombCount, 0, 10);
            _genArmoredCount = EditorGUILayout.IntSlider("🛡️ Zırhlı Adedi:", _genArmoredCount, 0, 10);
            _genRainbowCount = EditorGUILayout.IntSlider("🌈 Rainbow Adedi:", _genRainbowCount, 0, 10);

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if(GUILayout.Button("🎲 Level ve Shooter Dizilimi Üret", GUILayout.Height(26)))
            {
                Undo.RecordObject(_data, "Generate Custom Level");
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
