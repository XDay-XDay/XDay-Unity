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

using System.Buffers;
using UnityEngine;

namespace XDay.WorldAPI.Tile.Editor
{
    public enum ClipLODType
    {
        ClipAll,
        ClipAny,
    }

    //控制地表tile的哪些多边形被裁剪
    public class ClipMask
    {
        public ClipMask(string name, int resolution, float tileWidth, float tileHeight, Vector3 tilePosition, ArrayPool<Color32> arrayPool, bool[,] clipGrids)
        {
            if (clipGrids == null)
            {
                clipGrids = new bool[resolution, resolution];
            }
            mClippedGrids = clipGrids;
            mMaskEditor = new MaskEditor();
            mMaskEditor.Init(name, resolution, resolution, tileWidth, tileHeight, tilePosition, null, GetID, SetID, GetColor, arrayPool);
            for (int i = 0; i < resolution; ++i)
            {
                for (int j = 0; j < resolution; ++j)
                {
                    SetID(j, i, GetID(j, i));
                }
            }
        }

        public void OnDestroy()
        {
            mMaskEditor?.OnDestroy();
        }

        public void Clear()
        {
            int rows = mClippedGrids.GetLength(0);
            int cols = mClippedGrids.GetLength(1);
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; ++j)
                {
                    mClippedGrids[i, j] = false;
                }
            }
            mMaskEditor?.RefreshTexture();
        }

        public void Show(bool visible)
        {
            mMaskEditor?.Show(visible);
        }

        public void SetClipped(Vector3 worldPos, int brushSize, bool clipped)
        {
            mMaskEditor.SetPixel(worldPos, brushSize, clipped ? 1 : 0);
        }

        public bool IsClipped(int x, int y)
        {
            return mClippedGrids[y, x];
        }

        void SetID(int x, int y, int id)
        {
            mClippedGrids[y, x] = id == 0 ? false : true;
        }

        int GetID(int x, int y)
        {
            return mClippedGrids[y, x] ? 1 : 0;
        }

        Color32 GetColor(int id)
        {
            return id == 0 ? new Color32(0, 0, 0, 0) : new Color32(255, 0, 0, 170);
        }

        public bool[,] clippedGrids { get { return mClippedGrids; } }

        bool[,] mClippedGrids;
        MaskEditor mMaskEditor;
    }
}
