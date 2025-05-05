/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal sealed partial class TileSystem
    {
        public TexturePainter TexturePainter => m_TexturePainter;
        public VertexHeightPainter VertexHeightPainter => m_VertexHeightPainter;
        public IResourceGroupSystem ResourceGroupSystem => m_ResourceGroupSystem;

        public override List<UIControl> GetSceneViewControls()
        {
            return m_Controls;
        }

        public TileObject CreateTile(Vector3 pos, string path)
        {
            var descriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(path, World);
            if (descriptor == null)
            {
                return null;
            }

            var n = 0;
            foreach (var tile in m_Tiles)
            {
                if (tile != null)
                {
                    ++n;
                }
            }

            return new TileObject(
                World.AllocateObjectID(),
                n, 
                true, 
                descriptor, 
                WorldObjectVisibility.Visible, 
                pos, 
                Quaternion.identity, 
                null, 
                false);
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            if (selected)
            {
                SetAction(m_Action);
            }
            else
            {
                m_Indicator.Visible = false;
                Tools.hidden = false;
            }
        }

        protected override void InspectorGUIInternal()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "地表层");
            if (m_Show)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                EditorHelper.IndentLayout(() =>
                {
                    m_PluginSystemEditor.InspectorGUI(m_PluginLODSystem, null, "LOD", null);

                    switch (m_Action)
                    {
                        case Action.SetMaterial:
                            DrawMaterialConfigs();
                            break;
                        case Action.PaintTexture:
                            m_TexturePainter.InspectorGUI();
                            break;
                        case Action.PaintHeight:
                            m_VertexHeightPainter.InspectorGUI();
                            break;
                        case Action.SetTile:
                            m_ResourceGroupSystem.InspectorGUI();
                            break;
                    }

                    m_ShowTileSetting = EditorGUILayout.Foldout(m_ShowTileSetting, "地块");
                    if (m_ShowTileSetting)
                    {
                        GUI.enabled = Mathf.IsPowerOfTwo((int)m_TileHeight) && m_TileWidth == m_TileHeight;

                        EditorHelper.IndentLayout(() =>
                        {
                            m_GameTileType = (GameTileType)EditorGUILayout.EnumPopup("地块类型", m_GameTileType);
                            if (m_GameTileType == GameTileType.TerrainLOD)
                            {
                                m_TerrainLODMorphRatio = Mathf.Clamp01(EditorGUILayout.FloatField("LOD插值比例", m_TerrainLODMorphRatio));
                            }
                        });

                        GUI.enabled = true;
                    }
                });

                EditorGUILayout.EndScrollView();
            }
        }

        protected override void SceneGUISelectedInternal()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1 && evt.control)
                {
                    SetAction(Action.Select);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha2 && evt.control)
                {
                    SetAction(Action.PaintTexture);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha3 && evt.control)
                {
                    SetAction(Action.SetMaterial);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha4 && evt.control)
                {
                    SetAction(Action.SetTile);
                    evt.Use();
                }
            }

            m_Indicator.Visible = false;
            switch (m_Action)
            {
                case Action.SetMaterial:
                    EditMaterial();
                    break;
                case Action.PaintTexture:
                    m_TexturePainter.SceneGUI();
                    HandleUtility.AddDefaultControl(0);
                    break;
                case Action.PaintHeight:
                    m_VertexHeightPainter.SceneGUI();
                    HandleUtility.AddDefaultControl(0);
                    break;
                case Action.SetTile:
                    SetTile();
                    break;
                default:
                    if (m_Action != Action.Select)
                    {
                        Debug.Assert(false, $"todo {m_Action}");
                    }
                    break;
            }
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CreateControls();

                DrawOperation();

                DrawTilingButton();

                if (m_Action == Action.PaintTexture)
                {
                    m_TexturePainter.DrawSceneGUIControls();
                }
                else if (m_Action == Action.PaintHeight)
                {
                    m_VertexHeightPainter.DrawSceneGUIControls();
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawTooltips();
        }

        private void CreateControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new List<UIControl>();

                m_OperationPopup = new Popup("操作", "", 140);
                m_Controls.Add(m_OperationPopup);

                var texturePainterControls = m_TexturePainter.CreateSceneGUIControls();
                m_Controls.AddRange(texturePainterControls);

                var vertexPainterControls = m_VertexHeightPainter.CreateSceneGUIControls();
                m_Controls.AddRange(vertexPainterControls);

                m_ButtonTiling = EditorWorldHelper.CreateImageButton("tiling.png", "平铺到全地图");
                m_Controls.Add(m_ButtonTiling);
            }
        }

        private void TilingAll()
        {
            var assetPath = m_ResourceGroupSystem.SelectedGroup.SelectedPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Error", "Asset path is null", "OK");
                return;
            }

            UndoSystem.NextGroupAndJoin();
            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var tile = GetTile(x, y);
                    UndoSystem.DestroyObject(tile, "Destroy Tile", ID, lod: 0);
                    
                    tile = CreateTile(CoordinateToUnrotatedPosition(x, y), assetPath);
                    UndoSystem.CreateObject(tile, World.ID, "Create Tile", ID, lod: 0);
                }
            }
            UndoSystem.NextGroupAndJoin();
        }

        private void UpdateIndicator(GameObject model, Vector3 pos, bool clearTile)
        {
            if (model == null || clearTile)
            {
                m_Indicator.Visible = false;
            }
            else
            {
                m_Indicator.Prefab = AssetDatabase.GetAssetPath(model);
                m_Indicator.Visible = true;
                m_Indicator.Rotation = model.transform.rotation * m_Rotation;

                var coord = RotatedPositionToCoordinate(pos.x, pos.z);
                m_Indicator.Position = CoordinateToRotatedPosition(coord.x, coord.y);
            }
        }

        private void SetAction(Action newAction)
        {
            if (m_Action == Action.PaintTexture && 
                newAction != Action.PaintTexture)
            {
                m_TexturePainter.SaveToFile();
            }
            m_Action = newAction;
            if (m_Action == Action.Select)
            {
                Tools.hidden = false;
            }
            else
            {
                Tools.hidden = true;
            }
        }

        private void DrawTilingButton()
        {
            if (m_ButtonTiling.Render(Inited && m_Action != Action.PaintTexture))
            {
                if (EditorUtility.DisplayDialog("平铺", "继续?", "确定", "取消"))
                {
                    TilingUseSelectedGroup();
                }
            }
        }

        private void DrawOperation()
        {
            SetAction((Action)m_OperationPopup.Render((int)m_Action, m_ActionNames, 30));
        }

        private void SetTile()
        {
            var evt = Event.current;
            var pos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            if (evt.alt == false &&
                evt.button == 0 &&
                (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag))
            {
                GetTileCoordinatesInRange(pos, out var minX, out var minY, out var maxX, out var maxY);
                if (evt.type == EventType.MouseDown)
                {
                    UndoSystem.NextGroup();
                }

                for (var y = minY; y <= maxY; ++y)
                {
                    for (var x = minX; x <= maxX; ++x)
                    {
                        if (x >= 0 && x < m_XTileCount && y >= 0 && y < m_YTileCount)
                        {
                            var tile = GetTile(x, y);

                            if (!evt.control)
                            {
                                var path = m_ResourceGroupSystem.SelectedResourcePath;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (tile == null || 
                                        tile.AssetPath != path)
                                    {
                                        UndoSystem.DestroyObject(tile, "Destroy Tile", ID, lod: 0);
                                        tile = CreateTile(CoordinateToUnrotatedPosition(x, y), path);
                                        UndoSystem.CreateObject(tile, World.ID, "Create Tile", ID, lod: 0);
                                    }
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "Invalid resource selected", "OK");
                                    evt.Use();
                                }
                            }
                            else
                            {
                                UndoSystem.DestroyObject(tile, "Destroy Tile", ID, 0);
                            }
                        }
                    }
                }
            }

            UpdateIndicator(m_ResourceGroupSystem.SelectedPrefab, pos, evt.control);

            HandleUtility.AddDefaultControl(0);
        }

        private void DrawTooltips()
        {
            if (m_Action == Action.PaintTexture)
            {
                m_TexturePainter.DrawTooltips();
            }
            else if (m_Action == Action.PaintHeight)
            {
                m_VertexHeightPainter.DrawTooltips();
            }
            EditorGUILayout.LabelField($"地块总数: {m_XTileCount}X{m_YTileCount}个");
            EditorGUILayout.LabelField($"单个地块大小: {m_TileWidth}X{m_TileHeight}米");
        }

        internal void GenerateHeightMeshes()
        {
            m_TileMeshCreator.GenerateHeightMeshes();
        }

        private int m_Range = 1;
        private bool m_Show = true;
        private readonly PluginLODSystemEditor m_PluginSystemEditor = new();
        private TexturePainter m_TexturePainter = new();
        private VertexHeightPainter m_VertexHeightPainter = new();
        private IResourceGroupSystem m_ResourceGroupSystem = IResourceGroupSystem.Create(true);
        private Vector2 m_ScrollPos;
        private Popup m_OperationPopup;
        private List<UIControl> m_Controls;
        private ImageButton m_ButtonTiling;
        private Action m_Action = Action.Select;
        private const int m_Version = 3;

        private enum Action
        {
            Select,
            PaintTexture,
            SetMaterial,
            SetTile,
            PaintHeight,
        }

        private string[] m_ActionNames =
        {
            "选择",
            "贴图绘制",
            "材质设置",
            "刷地块",
            "绘制顶点高度",
        };
    }
}

//XDay
