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
using XDay.UtilityAPI;

namespace XDay.GamePlayAPI
{
    internal class FogSystem
    {
        public int Resolution => m_Resolution;
        internal int[] Visibility => m_Visibility;
        public Texture2D FogMask => m_View.FogMask;
        public bool Enable => m_Enable;

        public void Init(int resolution, FixedVector3 min, FixedVector3 size, GameObject fogPrefab)
        {
            m_Min = min;
            m_Size = size;
            m_Resolution = resolution;
            m_Visibility = new int[resolution * resolution];
            for (var i = 0; i < m_Visibility.Length; ++i)
            {
                m_Visibility[i] = -1;
            }

            var fogObject = new GameObject();
            m_View = fogObject.AddComponent<FogView>();
            m_View.Init(this, size, fogPrefab);
        }

        public void Uninit()
        {
            Helper.DestroyUnityObject(m_View.gameObject);
            m_View = null;
        }

        public void ResetVisibility()
        {
            if (!m_Enable)
            {
                return;
            }

            for (var i = 0; i < m_Visibility.Length; ++i)
            {
                if (m_Visibility[i] > 0)
                {
                    m_Visibility[i] = 0;
                }
            }
        }

        public void LogicTick()
        {
            if (!m_Enable)
            {
                return;
            }

            if (m_Dirty)
            {
                m_Dirty = false;
                m_View.UpdateNewMask();
            }
        }

        public bool IsVisible(FixedVector3 center)
        {
            if (!m_Enable)
            {
                return true;
            }

            PositionToCoordinate(center, out var x, out var y);
            var xi = x.IntValue;
            var yi = y.IntValue;
            if (xi < 0 || yi < 0 || xi >= m_Resolution || yi >= m_Resolution)
            {
                return false;
            }
            var idx = yi * m_Resolution + xi;
            return m_Visibility[idx] > 0;
        }

        public void Open(FixedVector3 center, int radius)
        {
            if (!m_Enable)
            {
                return;
            }

            PositionToCoordinate(center, out var x, out var y);
            Open(x.IntValue, y.IntValue, radius);
        }

        public void Open(int x, int y, int radius)
        {
            if (!m_Enable)
            {
                return;
            }

            var minX = x - radius;
            var minY = y - radius;
            var maxX = x + radius;
            var maxY = y + radius;
            for (var i = minY; i <= maxY; i++)
            {
                for (var j = minX; j <= maxX; j++)
                {
                    if (i >= 0 && i < m_Resolution &&
                        j >= 0 && j < m_Resolution)
                    {
                        var dx = j - x;
                        var dy = i - y;
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            m_Visibility[i * m_Resolution + j] = 1;
                        }
                    }
                }
            }
            m_Dirty = true;
        }

        private void PositionToCoordinate(FixedVector3 pos, out FixedPoint x, out FixedPoint y)
        {
            x = (1 - (pos.X - m_Min.X) / m_Size.X) * m_Resolution;
            y = (1 - (pos.Z - m_Min.Z) / m_Size.Z) * m_Resolution;
        }

        private int m_Resolution;
        //-1表示未被访问过,0表示gray,>0表示可见
        private int[] m_Visibility;
        private FogView m_View;
        private FixedVector3 m_Min;
        private FixedVector3 m_Size;
        private bool m_Dirty = true;
        private bool m_Enable = true;

        [System.Flags]
        internal enum State
        {
            Dark = 0,
            White = 1,
            Gray = 2,
        }
    }
}
