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
using XDay.UtilityAPI.Math;
using XDay.WorldAPI.Editor;


namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TexturePainter
    {
        internal class TileInfo
        {
            public PaintInfo PaintInfo { get => m_PaintInfo; set => m_PaintInfo = value; }

            public TileInfo(Vector2Int coord)
            {
                m_Coord = coord;
            }

            public void Update()
            {
                m_PaintInfo.Update();
            }

            public void Prepare()
            {
                m_PaintInfo.ModifiedArea.Reset();
                m_PaintInfo.NewPixels.CopyTo(m_PaintInfo.OldPixels, 0);
            }

            public void EndPainting(TexturePainter painter, IntBounds2D overriddenArea)
            {
                var area = overriddenArea;
                if (overriddenArea.IsEmpty)
                {
                    area = m_PaintInfo.ModifiedArea;
                }

                if (!area.IsEmpty)
                {
                    var action = new UndoActionMaskPainting("Mask Painting",
                        UndoSystem.Group, 
                        m_PaintInfo.Texture.width, 
                        m_PaintInfo.OldPixels, 
                        m_PaintInfo.NewPixels, 
                        m_Coord, 
                        painter, 
                        area);
                    UndoSystem.PerformCustomAction(action, false);
                }
            }

            private readonly Vector2Int m_Coord;
            private PaintInfo m_PaintInfo;
        }

        internal class ImportSetting
        {
            public TextureImporterType Type { set; get; }
            public bool SRGBTexture { set; get; }
            public FilterMode Filter { set; get; }
            public Material Material { set; get; }
            public TextureImporterAlphaSource AlphaSource { set; get; }
            public bool MipMapEnabled { set; get; }
            public TextureImporterFormat Format { set; get; }
            public bool Readable { set; get; }
            public TextureWrapMode Wrap { set; get; }
            public TextureImporterCompression Compression { set; get; }
            public bool AlphaIsTransparency { set; get; }
        }

        internal class PaintInfo
        {
            public ImportSetting OriginalSetting => m_OriginalSetting;
            public Texture2D Texture => m_Texture;
            public bool Dirty { get => m_Dirty; set => m_Dirty = value; }
            public Color[] NewPixels => m_NewPixels;
            public Color[] OldPixels => m_OldPixels;
            public IntBounds2D ModifiedArea => m_ModifiedArea;

            public PaintInfo(Texture2D texture, ImportSetting setting)
            {
                m_Texture = texture;
                if (m_Texture != null)
                {
                    m_NewPixels = m_Texture.GetPixels();
                }
                m_OldPixels = (Color[])m_NewPixels.Clone();
                m_OriginalSetting = setting;
            }

            public void Update()
            {
                if (m_Texture != null)
                {
                    m_NewPixels = m_Texture.GetPixels();
                }
            }

            public void SetPixels(Vector2Int min, Vector2Int size, Color[] pixels)
            {
                var textureResolution = m_Texture.width;
                for (var i = 0; i < size.y; ++i)
                {
                    for (var j = 0; j < size.x; ++j)
                    {
                        var dstIdx = (min.y + i) * textureResolution + j + min.x;
                        var srcIdx = i * size.x + j;
                        m_NewPixels[dstIdx] = pixels[srcIdx];
                    }
                }

                m_Texture.SetPixels(min.x, min.y, size.x, size.y, pixels);
                UpdateDirtyArea(min.x, min.y, size.x, size.y);
            }

            public void SetPixels(Vector2Int min, Vector2Int size, Color32[] pixels)
            {
                var textureResolution = m_Texture.width;
                for (var i = 0; i < size.y; ++i)
                {
                    for (var j = 0; j < size.x; ++j)
                    {
                        var dstIdx = (min.y + i) * textureResolution + j + min.x;
                        var srcIdx = i * size.x + j;
                        m_NewPixels[dstIdx] = pixels[srcIdx];
                    }
                }

                m_Texture.SetPixels32(min.x, min.y, size.x, size.y, pixels);
                UpdateDirtyArea(min.x, min.y, size.x, size.y);
            }

            private void UpdateDirtyArea(int x, int y, int width, int height)
            {
                m_Dirty = true;
                m_ModifiedArea.AddRect(x, y, width, height);
                m_Texture.Apply();
            }

            private bool m_Dirty = false;
            private readonly ImportSetting m_OriginalSetting;
            private readonly Color[] m_OldPixels;
            private Color[] m_NewPixels;
            private readonly Texture2D m_Texture;
            private readonly IntBounds2D m_ModifiedArea = new();
        }

        internal class UndoActionMaskPainting : CustomUndoAction
        {
            public override bool CanJoin => false;
            public override int Size => (m_OriginalPixels.Length + m_NewPixels.Length) * 4;

            public UndoActionMaskPainting(string name,
                UndoActionGroup group,
                int maskTextureResolution,
                Color[] originalPixels,
                Color[] newPixels,
                Vector2Int tileCoord,
                TexturePainter painter,
                IntBounds2D bounds)
                : base(name, group)
            {
                var size = bounds.Size;
                var n = size.x * size.y;
                m_OriginalPixels = new Color32[n];
                m_NewPixels = new Color32[n];

                var min = bounds.Min;
                var max = bounds.Max;
                m_Bounds = new IntBounds2D(min, max);

                for (var y = min.y; y <= max.y; ++y)
                {
                    for (var x = min.x; x <= max.x; ++x)
                    {
                        var src = (y - min.y) * size.x + x - min.x;
                        var dst = y * maskTextureResolution + x;
                        m_OriginalPixels[src] = originalPixels[dst];
                        m_NewPixels[src] = newPixels[dst];
                    }
                }

                m_TileCoord = tileCoord;
                m_TexturePainter = painter;
            }

            public override bool Undo()
            {
                return SetData(m_OriginalPixels);
            }

            public override bool Redo()
            {
                return SetData(m_NewPixels);
            }

            protected override bool JoinInternal(CustomUndoAction other) { return false; }

            private bool SetData(Color32[] pixels)
            {
                var tile = m_TexturePainter.GetTileInfo(m_TileCoord.x, m_TileCoord.y);
                if (tile != null)
                {
                    tile.PaintInfo.SetPixels(m_Bounds.Min, m_Bounds.Size, pixels);
                    return true;
                }
                return false;
            }

            private readonly Vector2Int m_TileCoord;
            private readonly IntBounds2D m_Bounds;
            private readonly Color32[] m_OriginalPixels;
            private readonly Color32[] m_NewPixels;
            private readonly TexturePainter m_TexturePainter;
        }
    }
}

//XDay