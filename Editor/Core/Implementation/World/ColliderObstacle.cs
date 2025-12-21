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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Editor
{
    internal class ColliderObstacle : IObstacle
    {
        public bool IsValid => true;
        public List<Vector3> WorldPolygon => m_WorldPolyon;
        public Rect WorldBounds => m_WorldBounds;

        public ColliderObstacle(UnityEngine.Collider collider)
        {
            var bounds = collider.bounds;
            m_WorldBounds = bounds.ToRect();
            m_WorldPolyon.Add(m_WorldBounds.min.ToVector3XZ());
            m_WorldPolyon.Add(new Vector3(m_WorldBounds.xMin, 0, m_WorldBounds.yMax));
            m_WorldPolyon.Add(new Vector3(m_WorldBounds.xMax, 0, m_WorldBounds.yMax));
            m_WorldPolyon.Add(new Vector3(m_WorldBounds.xMax, 0, m_WorldBounds.yMin));
#if false
                //debug
                var obj = new GameObject(name);
                var dp = obj.AddComponent<DrawPolygon>();
                dp.Init(polygon);
#endif
        }

        private readonly List<Vector3> m_WorldPolyon = new();
        private Rect m_WorldBounds;
    }
}
