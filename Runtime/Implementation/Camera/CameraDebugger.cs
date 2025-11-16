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

namespace XDay.CameraAPI
{
    internal class CameraDebugger : MonoBehaviour
    {
        public Vector2 Min = new Vector2(-39.14f, -7.745f);
        public Vector2 Max = new Vector2(54.14f, 59.245f);
        public Rect VisibleAreas => m_VisibleArea;
        public Rect ExpandedVisibleAreas => m_ExpandedVisibleArea;

        public void Init(Camera camera, bool xy)
        {
            m_Camera = camera;
            m_XYPlane = xy;

            if (m_XYPlane)
            {
                m_GroundPlane = new Plane(Vector3.back, 0);
            }
            else
            {
                m_GroundPlane = new Plane(Vector3.up, 0);
            }
        }

        private void OnDrawGizmos()
        {
            var color = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3((Max.x + Min.x) * 0.5f, (Max.y + Min.y) * 0.5f, 0), new Vector3(Max.x - Min.x, Max.y - Min.y, 0));
            Gizmos.color = color;

            DebugDraw();
        }

        public void DebugDraw()
        {
            var oldColor = Gizmos.color;
            Gizmos.color = Color.red;
            if (m_XYPlane)
            {
                Gizmos.DrawWireCube(m_ExpandedVisibleArea.center.ToVector3XY(), m_ExpandedVisibleArea.size.ToVector3XY());
            }
            else
            {
                Gizmos.DrawWireCube(m_ExpandedVisibleArea.center.ToVector3XZ(), m_ExpandedVisibleArea.size.ToVector3XZ());
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

        private void Update()
        {
            if (m_Camera != null)
            {
                var cameraPos = m_Camera.transform.position;
                if (cameraPos != m_LastCameraPos)
                {
                    m_LastCameraPos = cameraPos;
                    m_VisibleArea = GetVisibleAreas(m_Camera);
                    m_ExpandedVisibleArea = Helper.ExpandRect(m_VisibleArea, m_ExpandSize);
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

        private Rect m_VisibleArea;
        private Rect m_ExpandedVisibleArea;
        private Vector3 m_LastCameraPos;
        private Plane m_GroundPlane;
        private Vector2 m_ExpandSize;
        private bool m_XYPlane;
        private Camera m_Camera;
    }
}
