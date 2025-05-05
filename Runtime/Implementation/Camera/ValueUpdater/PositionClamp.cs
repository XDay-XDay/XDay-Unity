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
        public bool EnableClampXZ { get => m_EnableHorizontalClamp; set => m_EnableHorizontalClamp = value; }
        public float PenetrationDistance { get => m_PenetrationDistance; set => m_PenetrationDistance = value; }

        public PositionClamp(string name, Transform parent, CameraDirection direction)
        {
            m_FocusObject = new GameObject(name);
            m_FocusObject.transform.SetParent(parent, worldPositionStays: false);
            m_Direction = direction;
        }

        public Vector3 Clamp(Vector3 cameraPos, Vector3 cameraForward, float cameraRotX, float minHeight, float maxHeight, Camera camera)
        {
            Vector3 pos;
            if (m_Direction == CameraDirection.XZ)
            {
                pos = ClampXZ(cameraPos, cameraForward, cameraRotX);
                pos = ClampY(pos, cameraForward, minHeight, maxHeight, camera);
            }
            else
            {
                pos = ClampXY(cameraPos, cameraForward, cameraRotX);
                pos = ClampZ(pos, cameraForward, minHeight, maxHeight, camera);
            }
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
            if (m_EnableHorizontalClamp)
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
                    pos = m_Position - cameraForward * Helper.FocalLengthFromAltitudeXZ(cameraRotX, cameraPos.y);
                }
            }

            if (m_FocusObject != null)
            {
                m_FocusObject.transform.position = m_Position;
            }

            return pos;
        }

        private Vector3 ClampXY(Vector3 cameraPos, Vector3 cameraForward, float cameraRotX)
        {
            m_Position = Helper.RayCastXYPlane(cameraPos, cameraForward);
            var pos = cameraPos;
            if (m_EnableHorizontalClamp)
            {
                var offset = m_EnableRestore ? new Vector2(m_PenetrationDistance, m_PenetrationDistance) : Vector2.zero;

                var boundsMin = m_AreaMin - offset;
                var boundsMax = m_AreaMax + offset;
                if (Helper.GT(boundsMin.x, m_Position.x) ||
                    Helper.GT(m_Position.x, boundsMax.x) ||
                    Helper.GT(boundsMin.y, m_Position.y) ||
                    Helper.GT(m_Position.y, boundsMax.y))
                {
                    m_Position = Helper.ClampPointInXYPlane(m_Position, boundsMin, boundsMax);
                    pos = m_Position - cameraForward * Helper.FocalLengthFromAltitudeXY(cameraRotX, -cameraPos.z);
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
                return Helper.FromFocusPointXZ(camera, focusPoint, y);
            }
            return pos;
        }

        private Vector3 ClampZ(Vector3 pos, Vector3 forward, float minHeight, float maxHeight, Camera camera)
        {
            float absHeight = Mathf.Abs(pos.z);
            if (absHeight < minHeight || absHeight > maxHeight)
            {
                var h = Mathf.Clamp(absHeight, minHeight, maxHeight);
                var focusPoint = Helper.RayCastXYPlane(pos, forward);
                return Helper.FromFocusPointXY(camera, focusPoint, h);
            }
            return pos;
        }

        private Vector3 m_Position;
        private GameObject m_FocusObject;
        private float m_PenetrationDistance = 1.0f;
        private bool m_EnableRestore = false;
        private bool m_EnableHorizontalClamp = false;
        private Vector2 m_AreaMin;
        private Vector2 m_AreaMax;
        private CameraDirection m_Direction;
    }
}


//XDay