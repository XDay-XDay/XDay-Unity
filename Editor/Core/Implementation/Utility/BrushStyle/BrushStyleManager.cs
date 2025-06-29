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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XDay.UtilityAPI.Editor
{
    internal partial class BrushStyleManager : IBrushStyleManager
    {
        public IBrushStyle SelectedStyle
        {
            get
            {
                if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Styles.Count)
                {
                    return GetStyle(m_SelectedIndex);
                }
                return null;
            }
        }

        public BrushStyleManager(string styleTextureFolder)
        {
            m_TextureRotation = new TextureRotation();
            m_StyleTextureFolder = styleTextureFolder;
            Refresh(false);
        }

        public void OnDestroy()
        {
            m_TextureRotation.OnDestroy();
            DestroyBrushes();
        }

        public void Rotate(float rotation, bool onlyAlpha)
        {
            (SelectedStyle as BrushStyle)?.UpdateRotation(m_TextureRotation, rotation, onlyAlpha);
        }

        public void ChangeBrushFolder(string path)
        {
            m_StyleTextureFolder = path;
            Refresh(true);
        }

        private BrushStyle GetStyle(int index)
        {
            if (index >= 0 && index < m_Styles.Count)
            {
                return m_Styles[index];
            }
            return null;
        }

        private void CreateStyle(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("Invalid brush texture");
                return;
            }

            m_Styles.Add(new BrushStyle(texture, m_TextureRotation));
            if (m_Styles.Count == 1)
            {
                m_SelectedIndex = 0;
            }
        }

        private void Refresh(bool refreshAssetDatabase)
        {
            if (Directory.Exists(m_StyleTextureFolder))
            {
                DestroyBrushes();

                if (refreshAssetDatabase)
                {
                    AssetDatabase.Refresh();
                }

                foreach (var path in GetFiles())
                {
                    var texture = EditorHelper.CreateReadableTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                    texture.wrapMode = TextureWrapMode.Clamp;
                    CreateStyle(texture);
                    Object.DestroyImmediate(texture);
                }
            }
        }

        private void DestroyBrushes()
        {
            foreach (var style in m_Styles)
            {
                style.OnDestroy(true);
            }
            m_Styles.Clear();
            m_SelectedIndex = -1;
        }

        private string[] GetFiles()
        {
            return Directory.EnumerateFiles(m_StyleTextureFolder, "*.*", SearchOption.AllDirectories)
                .Where(
                path => 
                path.EndsWith(".png") || 
                path.EndsWith(".tga"))
                .ToArray();
        }

        private const int m_BlurPassCountMax = 20;
        private bool m_ShowStyles = true;
        private int m_BlurPassCount = 1;
        private bool m_ShowBlur = true;
        private TextureRotation m_TextureRotation;
        private readonly List<BrushStyle> m_Styles = new();
        private string m_StyleTextureFolder;
        private int m_SelectedIndex = -1;
    }
}

//XDay