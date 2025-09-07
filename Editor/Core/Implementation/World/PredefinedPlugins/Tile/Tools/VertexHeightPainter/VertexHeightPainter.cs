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

using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using System.Collections.Generic;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class VertexHeightPainter : ISerializable
    {
        public TileSystem TileSystem => m_TileSystem;
        public string TypeName => "VertexHeightPainter";

        public VertexHeightPainter()
        {
        }

        public void Init(TileSystem tileSystem, string brushFolder)
        {
            m_ShowBrush = EditorPrefs.GetBool(TileDefine.VERTEX_PAINT_SHOW_BRUSH, true);
            m_Show = EditorPrefs.GetBool(TileDefine.VERTEX_PAINT_SHOW, true);
            m_PaintHeightParameters.Intensity = EditorPrefs.GetFloat(TileDefine.VERTEX_PAINT_INTENSITY, 0.01f);
            m_PaintHeightParameters.PaintInOneTile = EditorPrefs.GetBool(TileDefine.VERTEX_PAINT_ONE_TILE, false);
            m_PaintHeightParameters.Range = EditorPrefs.GetFloat(TileDefine.VERTEX_PAINT_RANGE, 70);
            m_PaintHeightParameters.TargetHeight = EditorPrefs.GetFloat(TileDefine.VERTEX_PAINT_TARGET_HEIGHT, 10);
            m_PaintHeightParameters.SupressVertexHeightThreshold = EditorPrefs.GetFloat(TileDefine.VERTEX_PAINT_SUPPRESS_VERTEX_HEIGHT_THRESHOLD, 0.1f);
            m_PaintHeightParameters.SmoothBrush = EditorPrefs.GetBool(TileDefine.VERTEX_PAINT_SMOOTH_BRUSH, false);
            m_PaintHeightParameters.Mode = (HeightMode)EditorPrefs.GetInt(TileDefine.VERTEX_PAINT_MODE, (int)HeightMode.ChangeHeight);
            m_PaintHeightParameters.KeepEdgeVertexHeight = EditorPrefs.GetBool(TileDefine.VERTEX_PAINT_KEEP_EDGE_VERTEX_HEIGHT, false);

            m_TileSystem = tileSystem;
            m_BrushStyleManager = IBrushStyleManager.Create(brushFolder);
        }

        public void OnDestroy()
        {
            m_Indicator.OnDestroy();
            m_BrushStyleManager.OnDestroy();
        }

        public void ChangeBrushFolder(string path)
        {
            m_BrushStyleManager.ChangeBrushFolder(path);
        }

        public List<UIControl> CreateSceneGUIControls()
        {
            List<UIControl> controls = new();

            m_BrushStrengthField = new FloatField("强度", "", 100);
            controls.Add(m_BrushStrengthField);

            m_BrushSizeField = new FloatField("大小", "", 80);
            controls.Add(m_BrushSizeField);

            m_SuppressVertexHeightField = new FloatField("忽略高度", "", 100);
            controls.Add(m_SuppressVertexHeightField);

            m_TargetHeightField = new FloatField("目标高度", "将地表最高高度设置到指定值", 100);
            controls.Add(m_TargetHeightField);

            m_ResolutionField = new IntField("分辨率", "单个Tile的Mesh分辨率", 100);
            controls.Add(m_ResolutionField);

            m_HeightModeField = new EnumPopup("高度模式", "", 150);
            controls.Add(m_HeightModeField);

            m_ClipLODTypeField = new EnumPopup("裁剪类型", "LOD裁剪类型", 100);
            controls.Add(m_ClipLODTypeField);

            m_ActiveLODSelectField = new Popup("当前LOD", "显示LOD选择", 100);
            controls.Add(m_ActiveLODSelectField);

            m_SmoothBrushButton = EditorWorldHelper.CreateToggleImageButton(true, "smooth.png", "是否使用平滑笔刷");
            controls.Add(m_SmoothBrushButton);

            m_PaintInOneTileButton = EditorWorldHelper.CreateToggleImageButton(false, "single.png", "是否只绘制鼠标位置所在的Tile");
            controls.Add(m_PaintInOneTileButton);

            m_KeepEdgeVertexHeightButton = EditorWorldHelper.CreateToggleImageButton(false, "lock.png", "绘制顶点时保持Tile边界顶点不被修改");
            controls.Add(m_KeepEdgeVertexHeightButton);

            m_FixEdgeNormalButton = EditorWorldHelper.CreateImageButton("fix.png", "修复tile边界顶点的法线,保证tile之间衔接部分的法线是正确的");
            controls.Add(m_FixEdgeNormalButton);

            m_SuppressVertexHeightButton = EditorWorldHelper.CreateImageButton("sweep.png", "将高度低于某指定值的顶点高度设置为0,保证自动减面时能减少更多顶点");
            controls.Add(m_SuppressVertexHeightButton);

            m_GenerateHeightMeshesButton = EditorWorldHelper.CreateImageButton("generate.png", "生成运行时Mesh");
            controls.Add(m_GenerateHeightMeshesButton);

            m_SwitchMeshLODButton = EditorWorldHelper.CreateImageButton("switch.png", "切换Mesh LOD");
            controls.Add(m_SwitchMeshLODButton);

            m_RestoreMeshButton = EditorWorldHelper.CreateImageButton("restore.png", "恢复成编辑器Mesh");
            controls.Add(m_RestoreMeshButton);

            return controls;
        }

        public void SceneGUI()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.RightBracket)
                {
                    m_PaintHeightParameters.Range -= m_RangeChange;
                }
                else if (evt.keyCode == KeyCode.LeftBracket)
                {
                    m_PaintHeightParameters.Range += m_RangeChange;
                }
                SceneView.RepaintAll();
            }

            OnSceneGUIPaintHeight();

            if (evt.type == EventType.MouseDrag || evt.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }

            HandleUtility.AddDefaultControl(0);
        }

        public void InspectorGUI()
        {
            OnInspectorGUIPaintHeight();
        }

        public void DrawSceneGUIControls()
        {
            GUILayout.Space(40);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            m_PaintHeightParameters.Mode = (HeightMode)m_HeightModeField.Render(m_PaintHeightParameters.Mode, 60);

            DrawResolution();
            DrawFixEdgeNormal();
            DrawSuppressVertexHeight();
            DrawGenerateHeightMeshesButton();

            if (m_PaintHeightParameters.Mode == HeightMode.ChangeHeight)
            {
                DrawRange();
                DrawIntensity();
                DrawSmoothBrush();
                DrawPaintInOneTile();
                DrawKeepEdgeVertexHeight();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.SetHeight)
            {
                DrawRange();
                DrawIntensity();
                DrawTargetHeight();
                DrawSmoothBrush();
                DrawPaintInOneTile();
                DrawKeepEdgeVertexHeight();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.ResetHeight)
            {
                DrawRange();
                DrawPaintInOneTile();
                DrawKeepEdgeVertexHeight();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.SuppressVertex)
            {
                DrawRange();
                DrawSuppressVertexHeightField();
                DrawPaintInOneTile();
                DrawKeepEdgeVertexHeight();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.Smooth)
            {
                m_PaintHeightParameters.PaintInOneTile = false;
                DrawRange();
                DrawIntensity();
                DrawKeepEdgeVertexHeight();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.ResetEdgeHeight)
            {
                DrawRange();
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.SetClipMask)
            {
                DrawClipLODType();
            }

            GUILayout.Space(40);

            EditorGUILayout.EndHorizontal();

            //draw next row here
            DrawLODSelect();

            EditorGUILayout.EndVertical();
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("Version");

            m_PaintHeightParameters.Resolution = deserializer.ReadInt32("Resolution");
            m_PaintHeightParameters.ClipMaskSize = deserializer.ReadInt32("Clip Mask Size");
            m_PaintHeightParameters.ClipLODType = (ClipLODType)deserializer.ReadSingle("Clip LOD Type");
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "Version");
            EditorPrefs.SetBool(TileDefine.VERTEX_PAINT_SHOW_BRUSH, m_ShowBrush);
            EditorPrefs.SetBool(TileDefine.VERTEX_PAINT_SHOW, m_Show);
            EditorPrefs.SetFloat(TileDefine.VERTEX_PAINT_INTENSITY, m_PaintHeightParameters.Intensity);
            EditorPrefs.SetBool(TileDefine.VERTEX_PAINT_ONE_TILE, m_PaintHeightParameters.PaintInOneTile);
            EditorPrefs.SetFloat(TileDefine.VERTEX_PAINT_RANGE, m_PaintHeightParameters.Range);
            EditorPrefs.SetFloat(TileDefine.VERTEX_PAINT_TARGET_HEIGHT, m_PaintHeightParameters.TargetHeight);
            EditorPrefs.SetFloat(TileDefine.VERTEX_PAINT_SUPPRESS_VERTEX_HEIGHT_THRESHOLD, m_PaintHeightParameters.SupressVertexHeightThreshold);
            EditorPrefs.SetBool(TileDefine.VERTEX_PAINT_SMOOTH_BRUSH, m_PaintHeightParameters.SmoothBrush);
            EditorPrefs.SetInt(TileDefine.VERTEX_PAINT_MODE, (int)m_PaintHeightParameters.Mode);
            EditorPrefs.SetBool(TileDefine.VERTEX_PAINT_KEEP_EDGE_VERTEX_HEIGHT, m_PaintHeightParameters.KeepEdgeVertexHeight);
            serializer.WriteInt32(m_PaintHeightParameters.Resolution, "Resolution");
            serializer.WriteInt32(m_PaintHeightParameters.ClipMaskSize, "Clip Mask Size");
            serializer.WriteSingle((int)m_PaintHeightParameters.ClipLODType, "Clip LOD Type");
        }

        private void DrawRange()
        {
            m_PaintHeightParameters.Range = m_BrushSizeField.Render(m_PaintHeightParameters.Range, 30);
            m_PaintHeightParameters.Range = Mathf.Max(1, m_PaintHeightParameters.Range);
        }

        private void DrawIntensity()
        {
            m_PaintHeightParameters.Intensity = m_BrushStrengthField.Render(m_PaintHeightParameters.Intensity, 50, 0, 1);
        }

        private void DrawTargetHeight()
        {
            m_PaintHeightParameters.TargetHeight = m_TargetHeightField.Render(m_PaintHeightParameters.TargetHeight, 50);
        }

        private void DrawSmoothBrush()
        {
            if (m_SmoothBrushButton.Render(true, true))
            {
                m_PaintHeightParameters.SmoothBrush = m_SmoothBrushButton.Active;
            }
        }

        private void DrawPaintInOneTile()
        {
            if (m_PaintInOneTileButton.Render(true, true))
            {
                m_PaintHeightParameters.PaintInOneTile = m_PaintInOneTileButton.Active;
            }
        }

        private void DrawKeepEdgeVertexHeight()
        {
            if (m_KeepEdgeVertexHeightButton.Render(true, true))
            {
                m_PaintHeightParameters.KeepEdgeVertexHeight = m_KeepEdgeVertexHeightButton.Active;
            }
        }

        private void DrawFixEdgeNormal()
        {
            if (m_FixEdgeNormalButton.Render(true))
            {
                m_TileSystem.FixNormal();
            }
        }

        private void DrawSuppressVertexHeight()
        {
            if (m_SuppressVertexHeightButton.Render(true))
            {
                SuppressAllTileVertexHeight(m_TileSystem);
            }
        }

        private void DrawGenerateHeightMeshesButton()
        {
            if (m_GenerateHeightMeshesButton.Render(true))
            {
                m_TileSystem.GenerateHeightMeshes();
                //m_TileSystem.GeneratePlaneMeshes();
            }
        }

        private void DrawResolution()
        {
            m_PaintHeightParameters.Resolution = m_ResolutionField.Render(m_PaintHeightParameters.Resolution, 40);
            if (m_PaintHeightParameters.Resolution <= 0)
            {
                m_PaintHeightParameters.Resolution = 1;
            }
            if (m_PaintHeightParameters.Resolution > 256)
            {
                m_PaintHeightParameters.Resolution = 256;
            }
            if (!Helper.IsPOT(m_PaintHeightParameters.Resolution))
            {
                m_PaintHeightParameters.Resolution = Helper.NextPowerOf2(m_PaintHeightParameters.Resolution);
            }
        }

        private void DrawSuppressVertexHeightField()
        {
            m_PaintHeightParameters.SupressVertexHeightThreshold = m_SuppressVertexHeightField.Render(m_PaintHeightParameters.SupressVertexHeightThreshold, 50);
        }

        private void DrawClipLODType()
        {
            m_PaintHeightParameters.ClipLODType = (ClipLODType)m_ClipLODTypeField.Render(m_PaintHeightParameters.ClipLODType, 60);
        }

        private void DrawLODSelect()
        {
            EditorGUILayout.BeginHorizontal();
            if (m_LODNames == null || m_LODNames.Length != m_TileSystem.LODCount)
            {
                m_LODNames = new string[m_TileSystem.LODCount];
            }
            for (var i = 0; i < m_LODNames.Length; i++)
            {
                m_LODNames[i] = $"{i}";
            }

            m_ActiveLOD = Mathf.Clamp(m_ActiveLOD, 0, m_TileSystem.LODCount - 1);
            m_ActiveLOD = m_ActiveLODSelectField.Render(m_ActiveLOD, m_LODNames, 60);

            if (m_SwitchMeshLODButton.Render(m_TileSystem.Inited))
            {
                SwitchToLOD(m_ActiveLOD);
            }

            if (m_RestoreMeshButton.Render(m_TileSystem.Inited))
            {
                m_TileSystem.RetoreToEditorMesh();
            }
            EditorGUILayout.EndHorizontal();
        }

        public void DrawTooltips()
        {
            if (m_PaintHeightParameters.Mode == HeightMode.SetHeight)
            {
                EditorGUILayout.LabelField("按下Ctrl+左键拾取地表高度,设置到Target Height中");
            }
            else if (m_PaintHeightParameters.Mode == HeightMode.ChangeHeight)
            {
                EditorGUILayout.LabelField("按下Ctrl+左键降低地表高度");
            }
            EditorGUILayout.LabelField("按下[或]键修改笔刷大小");
        }

        private const int m_Version = 1;
        private readonly IBrushIndicator m_Indicator = IBrushIndicator.Create();
        private IBrushStyleManager m_BrushStyleManager;
        private bool m_ShowBrush = true;
        private bool m_Show = true;
        private TileSystem m_TileSystem;
        private PaintHeightParameter m_PaintHeightParameters = new();
        private FloatField m_BrushStrengthField;
        private FloatField m_BrushSizeField;
        private FloatField m_TargetHeightField;
        private FloatField m_SuppressVertexHeightField;
        private IntField m_ResolutionField;
        private EnumPopup m_HeightModeField;
        private EnumPopup m_ClipLODTypeField;
        private Popup m_ActiveLODSelectField;
        private ToggleImageButton m_SmoothBrushButton;
        private ToggleImageButton m_PaintInOneTileButton;
        private ToggleImageButton m_KeepEdgeVertexHeightButton;
        private ImageButton m_FixEdgeNormalButton;
        private ImageButton m_SuppressVertexHeightButton;
        private ImageButton m_GenerateHeightMeshesButton;
        private ImageButton m_SwitchMeshLODButton;
        private ImageButton m_RestoreMeshButton;
        private float m_RangeChange = 10;
        private string[] m_LODNames;

        internal class PaintHeightParameter
        {
            public int Resolution = 18;
            //in meter unit
            public float Range = 70;
            public float TargetHeight = 10;
            public float SupressVertexHeightThreshold = 0.1f;
            public float Intensity = 0.01f;
            public bool SmoothBrush = false;
            //不能改变edge vertex的高度
            public bool KeepEdgeVertexHeight = false;
            //只在鼠标选中的tile中绘制
            public bool PaintInOneTile = false;
            public int ClipMaskSize = 1;
            public ClipLODType ClipLODType = ClipLODType.ClipAll;
            public HeightMode Mode = HeightMode.ChangeHeight;
            public List<CoordInfo> TileCoords = new();
        }

        internal class CoordInfo
        {
            public Vector2Int tileCoord;
            public RectInt rangeInTile;
            public List<float> brushHeights = new();

            public bool IsEdgeModified(int resolution, TerrainTileEdgeCorner edgeOrCorner)
            {
                switch (edgeOrCorner)
                {
                    case TerrainTileEdgeCorner.BottomEdge:
                        return rangeInTile.yMin == 0;
                    case TerrainTileEdgeCorner.LeftBottomCorner:
                        return rangeInTile.xMin == 0 && rangeInTile.yMin == 0;
                    case TerrainTileEdgeCorner.RightBottomCorner:
                        return rangeInTile.xMax == resolution && rangeInTile.yMax == 0;
                    case TerrainTileEdgeCorner.RightEdge:
                        return rangeInTile.xMax == resolution;
                    case TerrainTileEdgeCorner.LeftEdge:
                        return rangeInTile.xMin == 0;
                    case TerrainTileEdgeCorner.LeftTopCorner:
                        return rangeInTile.xMin == 0 && rangeInTile.yMax == resolution;
                    case TerrainTileEdgeCorner.RightTopCorner:
                        return rangeInTile.xMax == resolution && rangeInTile.yMax == resolution;
                    case TerrainTileEdgeCorner.TopEdge:
                        return rangeInTile.yMax == resolution;
                    default:
                        Debug.Assert(false, "todo");
                        break;
                }
                return false;
            }
        }

        internal enum HeightMode
        {
            //增量式修改高度
            ChangeHeight,
            //设置到指定高度
            SetHeight,
            //将tile的顶点高度设置为0
            ResetHeight,
            //将tile的edge顶点高度设置为0
            ResetEdgeHeight,
            //平滑顶点
            Smooth,
            //将某个threshold以下的顶点高度设置为0,以便更好的优化mesh
            SuppressVertex,
            ShowTileResolution,
            SetTileResolution,
            SetClipMask,
        }
    }
}
