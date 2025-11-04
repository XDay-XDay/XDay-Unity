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
using System.IO;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TexturePainter : ISerializable
    {
        public int RangeChange { get => m_RangeChange; set => m_RangeChange = value; }
        public int Range { get => m_Range; set => m_Range = Mathf.Max(1, value); }
        public string MaskName { get => m_MaskName; set => m_MaskName = value; }
        public bool IgnoreAlpha { get => m_IgnoreAlpha; set => m_IgnoreAlpha = value; }
        public bool EnableRandomAngle { get => m_BrushRandomAngle; set => m_BrushRandomAngle = value; }
        public int Channel { get => m_Channel; set => m_Channel = value; }
        public bool EnableRotation => EnableRandomAngle || m_Angle != 0;
        public int Resolution { get => m_Resolution; set => m_Resolution = value; }
        public bool IsPainting => m_Tiles != null;
        public float Angle { get => m_Angle; set => m_Angle = value; }
        public bool NormalizeColor { get => m_NormalizeColor; set => m_NormalizeColor = value; }
        public float Intensity { get => m_Intensity; set => m_Intensity = Mathf.Clamp(value, 0.0001f, 1000f); }
        public float IntensityChange { get => m_IntensityChange; set => m_IntensityChange = value; }
        public TileSystem TileSystem => m_TileSystem;
        public string TypeName => "TexturePainter";

        public TexturePainter()
        {
        }

        public void Init(Action refreshFunc, TileSystem tileSystem, string brushFolder)
        {
            m_RangeChange = EditorPrefs.GetInt(TileDefine.TEXTURE_RANGE_CHANGE_SETTING, 10);
            m_Intensity = EditorPrefs.GetFloat(TileDefine.TEXTURE_INTENSITY_SETTING, 0.25f);
            m_IntensityChange = EditorPrefs.GetFloat(TileDefine.TEXTURE_INTENSITY_CHANGE_SETTING, 0.05f);
            m_Angle = EditorPrefs.GetFloat(TileDefine.TEXTURE_ANGLE, 0);
            m_BrushRandomAngle = EditorPrefs.GetBool(TileDefine.TEXTURE_BRUSH_RANDOM_ANGLE, true);
            m_ShowBrush = EditorPrefs.GetBool(TileDefine.TEXTURE_SHOW_BRUSH, true);
            m_Show = EditorPrefs.GetBool(TileDefine.TEXTURE_SHOW, true);
            m_Range = EditorPrefs.GetInt(TileDefine.TEXTURE_RANGE, 128);
            m_Channel = EditorPrefs.GetInt(TileDefine.TEXTURE_CHANNEL, 2);
            m_MaskCombineInfo.MinX = EditorPrefs.GetInt(TileDefine.TEXTURE_COMBINE_MINX);
            m_MaskCombineInfo.MinY = EditorPrefs.GetInt(TileDefine.TEXTURE_COMBINE_MINY);
            m_MaskCombineInfo.MaxX = EditorPrefs.GetInt(TileDefine.TEXTURE_COMBINE_MAXX);
            m_MaskCombineInfo.MaxY = EditorPrefs.GetInt(TileDefine.TEXTURE_COMBINE_MAXY);

            m_RefreshFunc = refreshFunc;
            m_TileSystem = tileSystem;
            m_BrushStyleManager = IBrushStyleManager.Create(brushFolder);
            m_ModifierManager = new TextureModifierManager(this);
        }

        public void OnDestroy()
        {
            m_Indicator.OnDestroy();
            m_ModifierManager.OnDestroy();
            m_BrushStyleManager.OnDestroy();
        }

        public void ChangeBrushFolder(string folder)
        {
            m_BrushStyleManager.ChangeBrushFolder(folder);
        }

        public List<UIControl> CreateSceneGUIControls()
        {
            List<UIControl> controls = new();
            m_ChannelPopup = new Popup("通道", "", 100);
            controls.Add(m_ChannelPopup);

            m_BrushStrengthField = new FloatField("强度", "", 100);
            controls.Add(m_BrushStrengthField);

            m_ButtonEndPaint = EditorWorldHelper.CreateImageButton("end.png", "结束绘制Mask贴图");
            controls.Add(m_ButtonEndPaint);

            m_BrushSizeField = new IntField("大小", "", 80);
            controls.Add(m_BrushSizeField);

            m_ButtonBeginPaint = EditorWorldHelper.CreateToggleImageButton(false, "start.png", "开始绘制Mask贴图");
            controls.Add(m_ButtonBeginPaint);

            m_ButtonPaintOneTile = EditorWorldHelper.CreateToggleImageButton(m_PaintOneTile, "single.png", "只绘制一个tile的贴图");
            controls.Add(m_ButtonPaintOneTile);

            m_ButtonResetMask = EditorWorldHelper.CreateToggleImageButton(false, "reset_mask.png", "重置Mask贴图");
            controls.Add(m_ButtonResetMask);

            m_ButtonCombineMask = EditorWorldHelper.CreateImageButton("combine_mask.png", "合并Mask贴图");
            controls.Add(m_ButtonCombineMask);

            m_ButtonSplitMask = EditorWorldHelper.CreateImageButton("split_mask.png", "分解Mask贴图");
            controls.Add(m_ButtonSplitMask);

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
                else if (evt.keyCode == KeyCode.C)
                {
                    ChangeBrushAngle(Angle - m_BrushAngleDelta);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.V)
                {
                    ChangeBrushAngle(Angle + m_BrushAngleDelta);
                    evt.Use();
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

            DrawCombineMaskButton();

            DrawSplitMaskButton();
        }

        public void DrawTooltips()
        {
            EditorGUILayout.LabelField("按下空格键旋转笔刷");
            EditorGUILayout.LabelField("按下[或]键修改笔刷大小");
            EditorGUILayout.LabelField("按下C或V键修改笔刷角度");
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
            if (m_ButtonEndPaint.Render(m_TileSystem.Inited && !string.IsNullOrEmpty(m_TileSystem.BrushFolder)))
            {
                End();
            }
        }

        private void DrawPaintOneTileButton()
        {
            m_ButtonPaintOneTile.Active = m_PaintOneTile;
            if (m_ButtonPaintOneTile.Render(m_TileSystem.Inited && !string.IsNullOrEmpty(m_TileSystem.BrushFolder)))
            {
                m_PaintOneTile = m_ButtonPaintOneTile.Active;
            }
        }

        private void DrawResetMaskButton()
        {
            m_ButtonResetMask.Render(m_TileSystem.Inited && !string.IsNullOrEmpty(m_TileSystem.BrushFolder));
        }

        private void DrawCombineMaskButton()
        {
            if (m_ButtonCombineMask.Render(m_TileSystem.Inited))
            {
                CombineMaskTextures();
            }
        }

        private void DrawSplitMaskButton()
        {
            if (m_ButtonSplitMask.Render(m_TileSystem.Inited))
            {
                SplitMaskTextures();
            }
        }

        private bool CombineMaskTextures()
        {
            if (IsPainting)
            {
                EditorUtility.DisplayDialog("出错了", "绘制过程中不能合并", "确定");
                return false;
            }

            var combined = false;
            var items = new List<ParameterWindow.Parameter> {
                                new ParameterWindow.IntParameter("Min X", "", m_MaskCombineInfo.MinX),
                                new ParameterWindow.IntParameter("Min Y", "", m_MaskCombineInfo.MinY),
                                new ParameterWindow.IntParameter("Max X", "", m_MaskCombineInfo.MaxX),
                                new ParameterWindow.IntParameter("Max Y", "", m_MaskCombineInfo.MaxY),
                            };
            ParameterWindow.Open("合并Mask", items, (parameters) =>
            {
                var ok = ParameterWindow.GetInt(parameters[0], out var minX);
                ok &= ParameterWindow.GetInt(parameters[1], out var minY);
                ok &= ParameterWindow.GetInt(parameters[2], out var maxX);
                ok &= ParameterWindow.GetInt(parameters[3], out var maxY);
                if (ok && IsValidRange(minX, minY, maxX, maxY))
                {
                    m_MaskCombineInfo.Set(minX, minY, maxX, maxY);

                    var textures = GetTileMaskTextures(minX, minY, maxX, maxY);

                    var combiner = new TextureCombiner();
                    m_MaskCombineInfo.CombinedTexture = combiner.Combine(textures, minX, minY, m_MaskCombineInfo.MaxX, m_MaskCombineInfo.MaxY, "Assets");
                    combined = true;
                    return true;
                }
                return false;
            });

            return combined;
        }

        private void SplitMaskTextures()
        {
            if (IsPainting)
            {
                EditorUtility.DisplayDialog("出错了", "绘制过程中不能分解", "确定");
                return;
            }

            var items = new List<ParameterWindow.Parameter> {
                                new ParameterWindow.IntParameter("Min X", "", m_MaskCombineInfo.MinX),
                                new ParameterWindow.IntParameter("Min Y", "", m_MaskCombineInfo.MinY),
                                new ParameterWindow.IntParameter("Max X", "", m_MaskCombineInfo.MaxX),
                                new ParameterWindow.IntParameter("Max Y", "", m_MaskCombineInfo.MaxY),
                                new ParameterWindow.ObjectParameter("Texture", "", m_MaskCombineInfo.CombinedTexture, typeof(Texture2D), false),
                            };
            ParameterWindow.Open("分解Mask", items, (parameters) =>
            {
                var ok = ParameterWindow.GetInt(parameters[0], out var minX);
                ok &= ParameterWindow.GetInt(parameters[1], out var minY);
                ok &= ParameterWindow.GetInt(parameters[2], out var maxX);
                ok &= ParameterWindow.GetInt(parameters[3], out var maxY);
                ok &= ParameterWindow.GetObject<Texture2D>(parameters[4], out var texture);
                if (ok && texture != null && IsValidRange(minX, minY, maxX, maxY))
                {
                    var combiner = new TextureCombiner();
                    var textures = combiner.Split(texture, minX, minY, maxX, maxY, m_Resolution);
                    if (textures.Count > 0)
                    {
                        SetTextures(textures, minX, minY, maxX, maxY);
                        m_MaskCombineInfo.CombinedTexture = null;
                        AssetDatabase.Refresh();
                        return true;
                    }
                }
                return false;
            });
        }

        private bool IsValidRange(int minX, int minY, int maxX, int maxY)
        {
            return minX >= 0 && minX < m_TileSystem.XTileCount &&
                minY >= 0 && minY < m_TileSystem.YTileCount &&
                maxX >= 0 && maxX < m_TileSystem.XTileCount &&
                maxY >= 0 && maxY < m_TileSystem.YTileCount && 
                minX <= maxX && 
                minY <= maxY;
        }

        private List<Texture2D> GetTileMaskTextures(int minX, int minY, int maxX, int maxY)
        {
            List<Texture2D> textures = new();
            for (var i = minY; i <= maxY; ++i)
            {
                for (var j = minX; j <= maxX; ++j)
                {
                    var tile = m_TileSystem.GetTile(j, i);
                    var texturePath = $"{Helper.RemoveExtension(tile.AssetPath)}.tga";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    Debug.Assert(texture);
                    textures.Add(texture);
                }
            }
            return textures;
        }

        private void SetTextures(List<byte[]> textures, int minX, int minY, int maxX, int maxY)
        {
            var idx = 0;
            for (var i = minY; i <= maxY; ++i)
            {
                for (var j = minX; j <= maxX; ++j)
                {
                    var tile = m_TileSystem.GetTile(j, i);
                    var texturePath = $"{Helper.RemoveExtension(tile.AssetPath)}.tga";
                    File.WriteAllBytes(texturePath, textures[idx++]);
                }
            }
        }

        private void DrawBeginPaintButton()
        {
            m_ButtonBeginPaint.Active = m_TextureToPaintInfo.Count != 0;
            if (m_ButtonBeginPaint.Render(m_TileSystem.Inited && !string.IsNullOrEmpty(m_TileSystem.BrushFolder)))
            {
                if (m_BrushStyleManager.Valid)
                {
                    Start();
                }
                else
                {
                    EditorUtility.DisplayDialog("出错了", "Brush目录没有设置,无法绘制", "确定");
                }
            }
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("Version");
            m_NormalizeColor = deserializer.ReadBoolean("Normalize Color");
            m_Resolution = deserializer.ReadInt32("Resolution");
            m_IgnoreAlpha = deserializer.ReadBoolean("Ignore Alpha");
            m_MaskName = deserializer.ReadString("Mask Name");
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "Version");
            serializer.WriteBoolean(m_NormalizeColor, "Normalize Color");
            serializer.WriteInt32(m_Resolution, "Resolution");
            serializer.WriteBoolean(m_IgnoreAlpha, "Ignore Alpha");
            serializer.WriteString(m_MaskName, "Mask Name");
            EditorPrefs.SetInt(TileDefine.TEXTURE_RANGE_CHANGE_SETTING, m_RangeChange);
            EditorPrefs.SetFloat(TileDefine.TEXTURE_INTENSITY_SETTING, m_Intensity);
            EditorPrefs.SetFloat(TileDefine.TEXTURE_INTENSITY_CHANGE_SETTING, m_IntensityChange);
            EditorPrefs.SetFloat(TileDefine.TEXTURE_ANGLE, m_Angle);
            EditorPrefs.SetBool(TileDefine.TEXTURE_BRUSH_RANDOM_ANGLE, m_BrushRandomAngle);
            EditorPrefs.SetBool(TileDefine.TEXTURE_SHOW_BRUSH, m_ShowBrush);
            EditorPrefs.SetBool(TileDefine.TEXTURE_SHOW, m_Show);
            EditorPrefs.SetInt(TileDefine.TEXTURE_RANGE, m_Range);
            EditorPrefs.SetInt(TileDefine.TEXTURE_CHANNEL, m_Channel);
            EditorPrefs.SetInt(TileDefine.TEXTURE_CHANNEL, m_Channel);
            EditorPrefs.SetInt(TileDefine.TEXTURE_COMBINE_MINX, m_MaskCombineInfo.MinX);
            EditorPrefs.SetInt(TileDefine.TEXTURE_COMBINE_MINY, m_MaskCombineInfo.MinY);
            EditorPrefs.SetInt(TileDefine.TEXTURE_COMBINE_MAXX, m_MaskCombineInfo.MaxX);
            EditorPrefs.SetInt(TileDefine.TEXTURE_COMBINE_MAXY, m_MaskCombineInfo.MaxY);
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
                        ChangeBrushAngle(newAngle);
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
            if (m_BrushStyleManager.SelectedStyle == null)
            {
                return;
            }

            var oldColor = Handles.color;
            Handles.color = Color.green;
            var size = (float)m_Range / m_Resolution * m_TileSystem.TileWidth;
            Handles.DrawWireDisc(center, Vector3.up, size * 0.5f);
            Handles.color = oldColor;

            m_Indicator.Texture = m_BrushStyleManager.SelectedStyle.GetTexture(EnableRotation);
            m_Indicator.Position = new Vector3(center.x, center.y + 0.25f, center.z);
            m_Indicator.Enabled = IsPainting;
            m_Indicator.Scale = size;
        }

        private void ChangeBrushAngle(float angle)
        {
            Angle = angle;
            while (Angle < 0)
            {
                Angle += 360f;
            }
            while (Angle > 360)
            {
                Angle -= 360f;
            }
            m_BrushStyleManager.Rotate(Angle, Channel < 4);
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
        private const int m_Version = 2;
        private FloatField m_BrushStrengthField;
        private IntField m_BrushSizeField;
        private ImageButton m_ButtonEndPaint;
        private ToggleImageButton m_ButtonBeginPaint;
        private ToggleImageButton m_ButtonPaintOneTile;
        private ToggleImageButton m_ButtonResetMask;
        private ImageButton m_ButtonCombineMask;
        private ImageButton m_ButtonSplitMask;
        private Popup m_ChannelPopup;
        private readonly string[] m_ChannelNames = new string[] { "R", "G", "B", "A" };
        private const float m_BrushAngleDelta = 3f;
        private MaskCombineInfo m_MaskCombineInfo = new();
    }
}
