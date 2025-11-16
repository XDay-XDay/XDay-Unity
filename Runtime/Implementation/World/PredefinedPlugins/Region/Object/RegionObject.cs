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

namespace XDay.WorldAPI.Region
{
    internal class RegionObject
    {
        public string Name => m_Name;
        public Vector3 Position => m_Position;
        public int ConfigID => m_ConfigID;
        public string LOD0PrefabPath => m_LOD0PrefabPath;
        public string LOD1PrefabPath => m_LOD1PrefabPath;
        public Rect Bounds => m_Bounds;
        public bool Active { get => m_Active; set => m_Active = value; }
        public Color Color { get => m_Color; set => m_Color = value; }

        public RegionObject(string name, int configID, Vector3 position, 
            Color color, Rect bounds, string lod0PrefabPath, string lod1PrefabPath) 
        {
            m_Name = name;
            m_Color = color;
            m_Position = position;
            m_Bounds = bounds;
            m_ConfigID = configID;
            m_LOD0PrefabPath = lod0PrefabPath;
            m_LOD1PrefabPath = lod1PrefabPath;
        }

        public bool Intersect(ref Rect area)
        {
            if (m_Bounds.xMin > area.xMax ||
                m_Bounds.yMin > area.yMax ||
                area.xMin > m_Bounds.xMax ||
                area.yMin > m_Bounds.yMax)
            {
                return false;
            }
            return true;
        }

        private bool m_Active = false;
        private string m_Name;
        private Color m_Color;
        private Vector3 m_Position;
        private int m_ConfigID;
        private string m_LOD0PrefabPath;
        private string m_LOD1PrefabPath;
        private Rect m_Bounds;
    }
}
