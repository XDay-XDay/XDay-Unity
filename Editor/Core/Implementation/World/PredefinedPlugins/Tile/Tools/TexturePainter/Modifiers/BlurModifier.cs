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
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal class BlurModifier : TextureModifier
    {
        public override string DisplayName => "Blur";

        public BlurModifier(TexturePainter painter, int passCount)
            : base(painter)
        {
            m_PassCount = passCount;
        }

        public override void InspectorGUI()
        {
            m_PassCount = Mathf.Min(EditorGUILayout.IntField("Pass Count", m_PassCount), 15);
        }

        public override void Modify()
        {
            var textures = GetTileTextures();
            if (textures.Count > 0)
            {
                var blur = new TextureBlur();
                blur.Blur(m_PassCount, textures);
            }
        }

        private List<Texture2D> GetTileTextures()
        {
            var textures = new List<Texture2D>();
            var xTileCount = m_Painter.TileSystem.XTileCount;
            var yTileCount = m_Painter.TileSystem.YTileCount;
            for (var y = 0; y < yTileCount; ++y)
            {
                for (var x = 0; x < xTileCount; ++x)
                {
                    var tile = m_Painter.GetTileInfo(x, y);
                    if (tile != null)
                    {
                        if (tile.PaintInfo.Texture != null)
                        {
                            textures.Add(tile.PaintInfo.Texture);
                        }
                    }
                }
            }
            return textures;
        }

        private int m_PassCount;
    }
}

//XDay