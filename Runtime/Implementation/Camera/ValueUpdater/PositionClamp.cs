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
using XDay.WorldAPI;

namespace XDay.CameraAPI
{
    internal class PositionClamp
    {
        public Vector2 FocusPointBoundsMin => m_FocusPointBoundsMin;
        public Vector2 FocusPointBoundsMax => m_FocusPointBoundsMax;
        public Vector3 Position => m_Position;
        public bool EnableRestore { get => m_EnableRestore; set => m_EnableRestore = value; }
        public bool EnableClampXZ { get => m_EnableHorizontalClamp; set => m_EnableHorizontalClamp = value; }
        public float PenetrationDistance { get => m_PenetrationDistance; set => m_PenetrationDistance = value; }

        public PositionClamp(string name, 
            Transform parent, 
            CameraDirection direction, 
            ICameraManipulator camera)
        {
            m_FocusObject = new GameObject(name);
            m_FocusObject.transform.SetParent(parent, worldPositionStays: false);
            m_Direction = direction;
            m_Calculator = new SLGCameraVisibleAreaCalculator(camera.Direction == CameraDirection.XY);
            m_CameraManipulator = camera;
        }

        public Vector3 Clamp(Vector3 cameraPos, 
            Vector3 cameraForward, 
            float cameraRotX, 
            float minHeight, 
            float maxHeight, 
            Camera camera)
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

        public void SetVisibleBounds(Vector2 min, Vector2 max)
        {
            m_VisibleBoundsMin = min;
            m_VisibleBoundsMax = max;
        }

        private Vector3 ClampXZ(Vector3 cameraPos, Vector3 cameraForward, float cameraRotX)
        {
            m_Position = Helper.RayCastXZPlane(cameraPos, cameraForward);
            var pos = cameraPos;
            if (m_EnableHorizontalClamp)
            {
                var offset = m_EnableRestore ? new Vector2(m_PenetrationDistance, m_PenetrationDistance) : Vector2.zero;

                GetAreaRange(cameraPos.y, false);
                var focusPointMin = m_FocusPointBoundsMin - offset;
                var focusPointMax = m_FocusPointBoundsMax + offset;
                if (Helper.LT(m_Position.x, focusPointMin.x) ||
                    Helper.GT(m_Position.x, focusPointMax.x) ||
                    Helper.LT(m_Position.z, focusPointMin.y) ||
                    Helper.GT(m_Position.z, focusPointMax.y))
                {
                    m_Position = Helper.ClampPointInXZPlane(m_Position, focusPointMin, focusPointMax);
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

                GetAreaRange(-cameraPos.z, true);
                var boundsMin = m_FocusPointBoundsMin - offset;
                var boundsMax = m_FocusPointBoundsMax + offset;
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
            if (Helper.LT(pos.y, minHeight, 0.01f) || 
                Helper.GT(pos.y, maxHeight, 0.01f))
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

        private void GetAreaRange(float y, bool xy)
        {
            if (Mathf.Approximately(m_LastCameraHeight, y))
            {
                return;
            }
            m_LastCameraHeight = y;

            m_VisibleSize = GetVisibleAreaSize(y, xy, m_CameraManipulator.Camera.fieldOfView, out var centerRatio);

            //由于相机视角的不同,相机中心点很可能不在视野范围中心
            m_FocusPointBoundsMin.x = m_VisibleBoundsMin.x + m_VisibleSize.x * centerRatio.x;
            m_FocusPointBoundsMin.y = m_VisibleBoundsMin.y + m_VisibleSize.y * centerRatio.y;
            m_FocusPointBoundsMax.x = m_VisibleBoundsMax.x - m_VisibleSize.x * (1 - centerRatio.x);
            m_FocusPointBoundsMax.y = m_VisibleBoundsMax.y - m_VisibleSize.y * (1 - centerRatio.y);
        }

        private Vector2 GetVisibleAreaSize(float cameraHeight, bool xy, float fov, out Vector2 centerRatio)
        {
            var camera = m_CameraManipulator.Camera;
            var cameraTransform = camera.transform;
            var oldPos = cameraTransform.position;
            var oldFOV = camera.fieldOfView;
            if (fov > 0)
            {
                camera.fieldOfView = fov;
            }
            if (xy)
            {
                cameraTransform.position = new Vector3(0, 0, -cameraHeight);
            }
            else
            {
                cameraTransform.position = new Vector3(0, cameraHeight, 0);
            }
            var area = m_Calculator.GetVisibleAreas(m_CameraManipulator.Camera);

            //z值会被忽略
            var focusPointWorldPosition = m_Calculator.GetFocusPoint(camera);
            centerRatio.x = Mathf.Clamp01((focusPointWorldPosition.x - area.xMin) / area.width);
            centerRatio.y = Mathf.Clamp01((focusPointWorldPosition.z - area.yMin) / area.height);

            cameraTransform.position = oldPos;
            camera.fieldOfView = oldFOV;
            return area.size;
        }

        private Vector3 m_Position;
        private GameObject m_FocusObject;
        private float m_PenetrationDistance = 1.0f;
        private bool m_EnableRestore = false;
        private bool m_EnableHorizontalClamp = false;
        //当相机拉到最远时可视范围
        private Vector2 m_VisibleBoundsMin;
        private Vector2 m_VisibleBoundsMax;
        //相机当前中心点移动范围
        private Vector2 m_FocusPointBoundsMin;
        private Vector2 m_FocusPointBoundsMax;
        private Vector2 m_VisibleSize;
        private float m_LastCameraHeight;
        private CameraDirection m_Direction;
        private ICameraVisibleAreaCalculator m_Calculator;
        private ICameraManipulator m_CameraManipulator;
    }
}


//XDay