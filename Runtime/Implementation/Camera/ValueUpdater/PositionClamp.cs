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

namespace XDay.CameraAPI
{
    internal class PositionClamp
    {
        public Vector2 AreaMin => m_AreaMin;
        public Vector2 AreaMax => m_AreaMax;
        public Vector3 Position => m_Position;
        public bool EnableRestore { get => m_EnableRestore; set => m_EnableRestore = value; }
        public bool EnableClampXZ { get => m_EnableClampXZ; set => m_EnableClampXZ = value; }
        public float PenetrationDistance { get => m_PenetrationDistance; set => m_PenetrationDistance = value; }

        public PositionClamp(string name, Transform parent)
        {
            m_FocusObject = new GameObject(name);
            m_FocusObject.transform.SetParent(parent, worldPositionStays: false);
        }

        public Vector3 Clamp(Vector3 cameraPos, Vector3 cameraForward, float cameraRotX, float minHeight, float maxHeight, Camera camera)
        {
            var pos = ClampXZ(cameraPos, cameraForward, cameraRotX);
            pos = ClampY(pos, cameraForward, minHeight, maxHeight, camera);

            return pos;
        }

        public void SetArea(Vector2 min, Vector2 max)
        {
            m_AreaMin = min;
            m_AreaMax = max;
        }

        private Vector3 ClampXZ(Vector3 cameraPos, Vector3 cameraForward, float cameraRotX)
        {
            m_Position = Helper.RayCastXZPlane(cameraPos, cameraForward);
            var pos = cameraPos;
            if (m_EnableClampXZ)
            {
                var offset = m_EnableRestore ? new Vector2(m_PenetrationDistance, m_PenetrationDistance) : Vector2.zero;

                var boundsMin = m_AreaMin - offset;
                var boundsMax = m_AreaMax + offset;
                if (Helper.GT(boundsMin.x, m_Position.x) ||
                    Helper.GT(m_Position.x, boundsMax.x) ||
                    Helper.GT(boundsMin.y, m_Position.z) ||
                    Helper.GT(m_Position.z, boundsMax.y))
                {
                    m_Position = Helper.ClampPointInXZPlane(m_Position, boundsMin, boundsMax);
                    pos = m_Position - cameraForward * Helper.FocalLengthFromAltitude(cameraRotX, cameraPos.y);
                }
            }

            if (m_FocusObject != null)
            {
                m_FocusObject.transform.position = m_Position;
            }

            return pos;
        }

        private Vector3 ClampY(Vector3 pos, Vector3 forward, float minHeight, float maxHeight, Camera camera)
        {
            if (pos.y < minHeight || pos.y > maxHeight)
            {
                var y = Mathf.Clamp(pos.y, minHeight, maxHeight);
                var focusPoint = Helper.RayCastXZPlane(pos, forward);
                return Helper.FromFocusPoint(camera, focusPoint, y);
            }
            return pos;
        }

        private Vector3 m_Position;
        private GameObject m_FocusObject;
        private float m_PenetrationDistance = 1.0f;
        private bool m_EnableRestore = false;
        private bool m_EnableClampXZ = false;
        private Vector2 m_AreaMin;
        private Vector2 m_AreaMax;
    }
}


//XDay