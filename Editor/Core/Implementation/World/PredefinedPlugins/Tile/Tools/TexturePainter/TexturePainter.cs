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
using System.Buffers;
using System.Collections.Generic;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Math;
using XDay.UtilityAPI.Editor;
using System;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TexturePainter : ISerializable
    {
        public int RangeChange { get => m_RangeChange; set => m_RangeChange = value; }
        public int Range { get => m_Range; set => m_Range = Mathf.Max(1, value);}
        public string MaskName { get => m_MaskName; set => m_MaskName = value; }
        public bool IgnoreAlpha { get => m_IgnoreAlpha; set => m_IgnoreAlpha = value; }
        public bool EnableRandomAngle { get => m_BrushRandomAngle; set => m_BrushRandomAngle = value; }
        public int Channel { get => m_Channel; set => m_Channel = value; }
        public bool EnableRotation => EnableRandomAngle || m_Angle != 0;
        public int Resolution { get => m_Resolution; set => m_Resolution = value; }
        public bool IsPainting => m_Tiles != null;
        public float Angle { get => m_Angle; set => m_Angle = value; }
        public bool NormalizeColor { get => m_NormalizeColor; set => m_NormalizeColor = value; }
        public float Intensity { get => m_Intensity; set => m_Intensity = Mathf.Clamp(value, 0.0001f, 1000f);}
        public float IntensityChange { get => m_IntensityChange; set => m_IntensityChange = value; }
        public TileSystem TileSystem => m_TileSystem;
        public string TypeName => "TexturePainter";

        public TexturePainter()
        {
        }

        public void Init(Action refreshFunc, TileSystem tileSystem)
        {
            m_RefreshFunc = refreshFunc;
            m_TileSystem = tileSystem;
            m_BrushStyleManager = IBrushStyleManager.Create(WorldHelper.GetBrushPath());
            m_ModifierManager = new TextureModifierManager(this);
        }

        public void OnDestroy()
        {
            m_Indicator.OnDestroy();
            m_ModifierManager.OnDestroy();
            m_BrushStyleManager.OnDestroy();
        }

        public List<UIControl> CreateSceneGUIControls()
        {
            List<UIControl> controls = new();
            m_ChannelPopup = new Popup("通道", "", 100);
            controls.Add(m_ChannelPopup);

            m_BrushStrengthField = new FloatField("强度", "", 100);
            controls.Add(m_BrushStrengthField);

            m_ButtonEndPaint = EditorWorldHelper.CreateImageButton("end.png", "");
            controls.Add(m_ButtonEndPaint);

            m_BrushSizeField = new IntField("大小", "", 80);
            controls.Add(m_BrushSizeField);

            m_ButtonBeginPaint = EditorWorldHelper.CreateImageButton("start.png", "");
            controls.Add(m_ButtonBeginPaint);

            m_ButtonPaintOneTile = EditorWorldHelper.CreateToggleImageButton(m_PaintOneTile, "single.png", "只绘制一个tile的贴图");
            controls.Add(m_ButtonPaintOneTile);

            m_ButtonResetMask = EditorWorldHelper.CreateToggleImageButton(false, "reset_mask.png", "重置Mask贴图");
            controls.Add(m_ButtonResetMask);

            return controls;
        }

        public void SceneGUI()
        {
            if (!IsPainting)
            {
                return;
            }

            var evt = Event.current;
            var pos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, m_TileSystem.World.CameraManipulator.Camera);

            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.RightBracket)
                {
                    Range -= RangeChange;
                }
                else if (evt.keyCode == KeyCode.LeftBracket)
                {
                    Range += RangeChange;
                }
                else if (EnableRandomAngle && evt.keyCode == KeyCode.Space)
                {
                    m_BrushStyleManager.Rotate(UnityEngine.Random.Range(0.0f, 360.0f), Channel < 4);
                }

                m_RefreshFunc?.Invoke();
            }

            DrawIndicator(pos);

            if (!evt.alt &&
                evt.button == 0 &&
                (EventType.MouseDown == evt.type || evt.type == EventType.MouseDrag))
            {
                if (EventType.MouseDown == evt.type)
                {
                    Prepare();
                }

                if (m_Painting)
                {
                    if (m_ButtonResetMask.Active)
                    {
                        ResetMask(pos);
                    }
                    else
                    {
                        PaintInternal(pos, evt.control);
                    }
                }
            }

            if (evt.type == EventType.MouseUp && m_Painting)
            {
                EndPainting(new IntBounds2D());
            }

            if (evt.type == EventType.MouseDrag || evt.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }

            HandleUtility.AddDefaultControl(0);
        }

        public void InspectorGUI()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "Mask贴图绘制");
            if (m_Show)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                {
                    IgnoreAlpha = EditorGUILayout.ToggleLeft(new GUIContent("忽略Alpha通道", "如果Shader中不使用Alpha通道则勾选"), IgnoreAlpha);
                    m_ModifierManager.InspectorGUI();
                    DrawStyle();
                }
                EditorGUILayout.EndVertical();
            }
        }

        public void DrawSceneGUIControls()
        {
            GUILayout.Space(40);

            DrawTexturePaintSettings();

            GUILayout.Space(40);

            DrawBeginPaintButton();

            DrawEndPaintButton();

            DrawPaintOneTileButton();

            DrawResetMaskButton();
        }

        public void DrawTooltips()
        {
            EditorGUILayout.LabelField("按下空格键旋转笔刷");
            EditorGUILayout.LabelField("按下[或]键修改笔刷大小");
            EditorGUILayout.LabelField($"Mask贴图在Shader中的名称:{MaskName}");
            EditorGUILayout.LabelField($"Mask贴图分辨率:{Resolution}");
        }

        private void DrawTexturePaintSettings()
        {
            Range = m_BrushSizeField.Render(Range, 30);
            Intensity = m_BrushStrengthField.Render(Intensity, 50);
            Channel = m_ChannelPopup.Render(Channel, m_ChannelNames, 50);
        }

        private void DrawEndPaintButton()
        {
            if (m_ButtonEndPaint.Render(m_TileSystem.Inited))
            {
                End();
            }
        }

        private void DrawPaintOneTileButton()
        {
            m_ButtonPaintOneTile.Active = m_PaintOneTile;
            if (m_ButtonPaintOneTile.Render(m_TileSystem.Inited))
            {
                m_PaintOneTile = m_ButtonPaintOneTile.Active;
            }
        }

        private void DrawResetMaskButton()
        {
            m_ButtonResetMask.Render(m_TileSystem.Inited);
        }

        private void DrawBeginPaintButton()
        {
            if (m_ButtonBeginPaint.Render(m_TileSystem.Inited))
            {
                Start();
            }
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("Version");
            m_NormalizeColor = deserializer.ReadBoolean("Normalize Color");
            m_RangeChange = deserializer.ReadInt32("Range Change");
            m_Intensity = deserializer.ReadSingle("Intensity");
            m_BrushRandomAngle = deserializer.ReadBoolean("Brush Random Angle");
            m_ShowBrush = deserializer.ReadBoolean("Show Brush");
            m_IntensityChange = deserializer.ReadSingle("Intensity Change");
            m_Angle = deserializer.ReadSingle("Angle");
            m_Resolution = deserializer.ReadInt32("Resolution");
            m_IgnoreAlpha = deserializer.ReadBoolean("Ignore Alpha");
            m_Show = deserializer.ReadBoolean("Show");
            m_MaskName = deserializer.ReadString("Mask Name");
            m_Range = deserializer.ReadInt32("Range");
            m_Channel = deserializer.ReadInt32("Channel");
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "Version");
            serializer.WriteBoolean(m_NormalizeColor, "Normalize Color");
            serializer.WriteInt32(m_RangeChange, "Range Change");
            serializer.WriteSingle(m_Intensity, "Intensity");
            serializer.WriteBoolean(m_BrushRandomAngle, "Brush Random Angle");
            serializer.WriteBoolean(m_ShowBrush, "Show Brush");
            serializer.WriteSingle(m_IntensityChange, "Intensity Change");
            serializer.WriteSingle(m_Angle, "Angle");
            serializer.WriteInt32(m_Resolution, "Resolution");
            serializer.WriteBoolean(m_IgnoreAlpha, "Ignore Alpha");
            serializer.WriteBoolean(m_Show, "Show");
            serializer.WriteString(m_MaskName, "Mask Name");
            serializer.WriteInt32(m_Range, "Range");
            serializer.WriteInt32(m_Channel, "Channel");
        }

        private void DrawStyle()
        {
            m_ShowBrush = EditorGUILayout.Foldout(m_ShowBrush, "笔刷");
            if (m_ShowBrush)
            {
                EditorHelper.IndentLayout(() =>
                {
                    GUI.enabled = !EnableRandomAngle;
                    var newAngle = EditorGUILayout.FloatField(new GUIContent("旋转角度", ""), Angle);
                    if (newAngle != Angle)
                    {
                        Angle = newAngle;
                        m_BrushStyleManager.Rotate(newAngle, Channel < 4);
                    }
                    GUI.enabled = true;

                    var enableRandomAngle = EditorGUILayout.ToggleLeft(new GUIContent("随机旋转角度", ""), EnableRandomAngle);
                    if (enableRandomAngle != EnableRandomAngle)
                    {
                        if (enableRandomAngle)
                        {
                            Angle = 0;
                        }
                        EnableRandomAngle = enableRandomAngle;
                    }

                    m_BrushStyleManager.InspectorGUI();
                });
            }
        }

        private void DrawIndicator(Vector3 center)
        {
            var oldColor = Handles.color;
            Handles.color = Color.green;
            var size = (float)m_Range / m_Resolution * m_TileSystem.TileWidth;
            Handles.DrawWireDisc(center, Vector3.up, size * 0.5f);
            Handles.color = oldColor;

            m_Indicator.Texture = m_BrushStyleManager.SelectedStyle.GetTexture(EnableRotation);
            m_Indicator.Position = new Vector3(center.x, center.y + 0.25f, center.z);
            m_Indicator.Enabled = true;
            m_Indicator.Scale = size;
        }

        private readonly IBrushIndicator m_Indicator = IBrushIndicator.Create();
        private bool m_ShowBrush = true;
        private float m_Angle = 0;
        private int m_Range = 128;
        private float m_Intensity = 0.25f;
        private bool m_NormalizeColor = true;
        private int m_Channel = 2;
        private TileInfo[] m_Tiles;
        private bool m_IgnoreAlpha = false;
        private readonly ArrayPool<Color> m_Pool = ArrayPool<Color>.Create();
        private TextureModifierManager m_ModifierManager;
        private Dictionary<Texture2D, PaintInfo> m_TextureToPaintInfo = new();
        private int m_RangeChange = 10;
        private string m_MaskName = "_SplatMask";
        private bool m_Show = true;
        private float m_IntensityChange = 0.05f;
        private bool m_Painting = false;
        private bool m_PaintOneTile = false;
        private IBrushStyleManager m_BrushStyleManager;
        private TileSystem m_TileSystem;
        private bool m_BrushRandomAngle = true;
        private Action m_RefreshFunc;
        private int m_Resolution = 512;
        private Dictionary<Texture2D, Material> m_TextureToMaterial;
        private Dictionary<Texture2D, ImportSetting> m_TextureToImportSetting;
        private Dictionary<Texture2D, List<Vector2Int>> m_TextureToTileCoordinates;
        private const int m_Version = 1;
        private FloatField m_BrushStrengthField;
        private IntField m_BrushSizeField;
        private ImageButton m_ButtonEndPaint;
        private ImageButton m_ButtonBeginPaint;
        private ToggleImageButton m_ButtonPaintOneTile;
        private ToggleImageButton m_ButtonResetMask;
        private Popup m_ChannelPopup;
        private readonly string[] m_ChannelNames = new string[] { "R", "G", "B", "A" };
    }
}

