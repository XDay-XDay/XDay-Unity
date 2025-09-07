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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Math;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class TextureModifierManager
    {
        public TextureModifierManager(TexturePainter painter)
        {
            m_Painter = painter;
            AddModifier(new BlurModifier(painter, 8));
        }

        public void OnDestroy()
        {
            foreach (var modifier in m_Modifiers)
            {
                modifier.OnDestroy();
            }
        }

        public void InspectorGUI()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "修改器");
            if (m_Show)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawModifiers();
                });
            }
        }

        private void AddModifier(TextureModifier plugin)
        {
            if (!m_Modifiers.Contains(plugin))
            {
                m_Modifiers.Add(plugin);

                m_Modifiers.Sort((TextureModifier p0, TextureModifier p1) =>
                {
                    return p0.Priority - p1.Priority;
                });
            }
        }

        private void DrawModifiers()
        {
            EditorGUILayout.BeginVertical();
            foreach (var modifier in m_Modifiers)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    modifier.Show = EditorGUILayout.Foldout(modifier.Show, new GUIContent(modifier.DisplayName, modifier.Tips));
                    GUI.enabled = m_Painter.IsPainting;
                    DrawApplyButton(modifier);
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();

                if (modifier.Show)
                {
                    EditorHelper.IndentLayout(() => {
                        modifier.InspectorGUI();
                    });
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void Apply(TextureModifier modifier)
        {
            modifier.Modify();

            var xTileCount = m_Painter.TileSystem.XTileCount;
            var yTileCount = m_Painter.TileSystem.YTileCount;
            for (var y = 0; y < yTileCount; ++y)
            {
                for (var x = 0; x < xTileCount; ++x)
                {
                    var tile = m_Painter.GetTileInfo(x, y);
                    tile?.Update();
                }
            }
        }

        private void DrawApplyButton(TextureModifier modifier)
        {
            if (GUILayout.Button("应用", GUILayout.MaxWidth(40)))
            {
                if (EditorUtility.DisplayDialog("警告", "继续?", "确定", "取消"))
                {
                    m_Painter.Prepare();
                    Apply(modifier);
                    m_Painter.EndPainting(new IntBounds2D(Vector2Int.zero, new Vector2Int(m_Painter.Resolution - 1, m_Painter.Resolution - 1)));
                }
            }
        }

        private readonly List<TextureModifier> m_Modifiers = new();
        private readonly TexturePainter m_Painter;
        private bool m_Show = true;
    }

    internal abstract class TextureModifier
    {
        public bool Show { get; set; } = true;
        public abstract string DisplayName { get; }
        public abstract string Tips { get; }
        public int Priority { get; set; } = 0;

        public TextureModifier(TexturePainter painter)
        {
            m_Painter = painter;
        }
        public virtual void OnDestroy() { }
        public abstract void InspectorGUI();
        public abstract void Modify();

        protected TexturePainter m_Painter;
    }
}


//XDay