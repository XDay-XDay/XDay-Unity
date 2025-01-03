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
using System.Threading.Tasks;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public class TextureScale
    {
        public void Scale(Texture2D texture, Vector2Int newSize, bool linearFilter = true)
        {
            ScaleInternal(texture, newSize, linearFilter);
        }

        public Texture2D CreateAndScale(Texture2D texture, Vector2Int newSize, bool linearFilter = true)
        {
            var copy = EditorHelper.CreateReadableTexture(texture);
            ScaleInternal(copy, newSize, linearFilter);
            return copy;
        }

        private void ScaleInternal(Texture2D texture, Vector2Int newSize, bool linearFilter)
        {
            m_OriginalData = texture.GetPixels();
            m_NewData = new Color[newSize.x * newSize.y];
            m_NewWidth = newSize.x;
            m_OriginalWidth = texture.width;
            m_Scale.x = (texture.width - (linearFilter ? 1 : 0)) / (float)newSize.x;
            m_Scale.y = (texture.height - (linearFilter ? 1 : 0)) / (float)newSize.y;

            var threadCount = Mathf.Min(SystemInfo.processorCount, newSize.y);
            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                var localIndex = i;

                var task = Task.Run(() =>
                {
                    ScaleTask(localIndex, threadCount, newSize.y, linearFilter);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            texture.Reinitialize(newSize.x, newSize.y);
            texture.SetPixels(m_NewData);
            texture.Apply();
        }

        private void ScaleTask(int threadIndex, int threadCount, int newHeight, bool linearFilter)
        {
            int rowsPerThread = newHeight / threadCount;
            Vector2Int range;
            if (threadIndex != threadCount - 1)
            {
                range = new Vector2Int(rowsPerThread * threadIndex, rowsPerThread * (threadIndex + 1));
            }
            else
            {
                range = new Vector2Int(rowsPerThread * threadIndex, newHeight);
            }

            if (linearFilter)
            {
                for (var y = range.x; y < range.y; y++)
                {
                    int yi = Mathf.FloorToInt(y * m_Scale.y);
                    var originalY = yi * m_OriginalWidth;
                    var nextOriginalY = originalY + m_OriginalWidth;
                    var ry = y * m_Scale.y - yi;
                    for (var x = 0; x < m_NewWidth; x++)
                    {
                        var xi = Mathf.FloorToInt(x * m_Scale.x);
                        var rx = x * m_Scale.x - xi;
                        var top = m_OriginalData[nextOriginalY + xi].Lerp(m_OriginalData[nextOriginalY + xi + 1], rx);
                        var bottom = m_OriginalData[originalY + xi].Lerp(m_OriginalData[originalY + xi + 1], rx);
                        m_NewData[y * m_NewWidth + x] = bottom.Lerp(top, ry);
                    }
                }
            }
            else
            {
                for (var y = range.x; y < range.y; y++)
                {
                    for (var x = 0; x < m_NewWidth; x++)
                    {
                        var idx = Mathf.FloorToInt(m_Scale.y * y * m_OriginalWidth + m_Scale.x * x);
                        m_NewData[y * m_NewWidth + x] = m_OriginalData[idx];
                    }
                }
            }
        }

        private Color[] m_NewData;
        private Color[] m_OriginalData;
        private Vector2 m_Scale;
        private int m_NewWidth;
        private int m_OriginalWidth;
    }
}


//XDay