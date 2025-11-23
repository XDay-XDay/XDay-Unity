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

using XDay.UtilityAPI;
using UnityEngine;

namespace XDay.WorldAPI
{
    internal class SLGCameraVisibleAreaCalculator : ICameraVisibleAreaCalculator
    {
        public Rect VisibleArea => m_VisibleArea;
        public Rect ExpandedArea => m_ExpandedArea;
        public Vector2 ExpandSize { get => m_ExpandSize; set => m_ExpandSize = value; }

        public SLGCameraVisibleAreaCalculator(bool xy)
        {
            m_XYPlane = xy;
            if (xy)
            {
                m_GroundPlane = new Plane(Vector3.back, 0);
            }
            else
            {
                m_GroundPlane = new Plane(Vector3.up, 0);
            }
        }

        public void DebugDraw()
        {
            var oldColor = Gizmos.color;
            Gizmos.color = Color.red;
            if (m_XYPlane)
            {
                Gizmos.DrawWireCube(m_ExpandedArea.center.ToVector3XY(), m_ExpandedArea.size.ToVector3XY());
            }
            else
            {
                Gizmos.DrawWireCube(m_ExpandedArea.center.ToVector3XZ(), m_ExpandedArea.size.ToVector3XZ());
            }

            Gizmos.color = Color.green;
            if (m_XYPlane)
            {
                Gizmos.DrawWireCube(m_VisibleArea.center.ToVector3XY(), m_VisibleArea.size.ToVector3XY());
            }
            else
            {
                Gizmos.DrawWireCube(m_VisibleArea.center.ToVector3XZ(), m_VisibleArea.size.ToVector3XZ());
            }
            Gizmos.color = oldColor;
        }

        public void Update(Camera camera)
        {
            if (camera != null)
            {
                var cameraPos = camera.transform.position;
                if (cameraPos != m_LastCameraPos)
                {
                    m_LastCameraPos = cameraPos;
                    m_VisibleArea = GetVisibleAreas(camera);
                    m_ExpandedArea = Helper.ExpandRect(m_VisibleArea, m_ExpandSize);
                }
            }
        }

        private Vector3 Raycast(Ray ray)
        {
            if (m_GroundPlane.Raycast(ray, out var distance))
            {
                return ray.GetPoint(distance);
            }
            return Vector3.zero;
        }

        public Vector3 GetFocusPoint(Camera camera)
        {
            return Raycast(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1)));
        }

        public Rect GetVisibleAreas(Camera camera)
        {
            var p0 = Raycast(camera.ViewportPointToRay(new Vector3(0, 1, 1)));
            var p1 = Raycast(camera.ViewportPointToRay(new Vector3(1, 1, 1)));
            var p2 = Raycast(camera.ViewportPointToRay(new Vector3(0, 0, 1)));
            var p3 = Raycast(camera.ViewportPointToRay(new Vector3(1, 0, 1)));

            if (m_XYPlane)
            {
                var maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
                var minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
                var maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);
                var minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            else
            {
                var maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
                var minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
                var maxZ = Mathf.Max(p0.z, p1.z, p2.z, p3.z);
                var minZ = Mathf.Min(p0.z, p1.z, p2.z, p3.z);
                return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
            }
        }

        /// <summary>
        /// 以梯形的下部分作为边框
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public Rect GetNarrowVisibleAreas(Camera camera)
        {
            var bottomLeft = Raycast(camera.ViewportPointToRay(new Vector3(0, 0, 1)));
            var topLeft = Raycast(camera.ViewportPointToRay(new Vector3(0, 1, 1)));
            var bottomRight = Raycast(camera.ViewportPointToRay(new Vector3(1, 0, 1)));

            if (m_XYPlane)
            {
                var minX = bottomLeft.x;
                var maxX = bottomRight.x;
                var minY = bottomLeft.y;
                var maxY = topLeft.y;
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            else
            {
                var minX = bottomLeft.x;
                var maxX = bottomRight.x;
                var minZ = bottomLeft.z;
                var maxZ = topLeft.z;
                return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
            }
        }

        private Rect m_VisibleArea;
        private Rect m_ExpandedArea;
        private Vector3 m_LastCameraPos;
        private Plane m_GroundPlane;
        private Vector2 m_ExpandSize;
        private readonly bool m_XYPlane;
    }
}

//XDay