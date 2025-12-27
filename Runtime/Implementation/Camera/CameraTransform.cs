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

namespace XDay.CameraAPI
{
    internal class CameraTransform
    {
        public bool IsLogicAltitudeChanged => !Mathf.Approximately(CurrentLogicPosition.y, StartPosition.y);
        public bool IsLogicTransformChanged => 
            CurrentLogicPosition != StartPosition || CurrentLogicRotation != StartRotation;

        public CameraTransform(Camera camera)
        {
            StartPosition = camera.transform.position;
            StartRotation = camera.transform.rotation;
            LastLogicPosition = StartPosition;
            LastLogicRotation = StartRotation;
            CurrentLogicPosition = StartPosition;
            CurrentLogicRotation = StartRotation;
            LastRenderPosition = StartPosition;
            LastRenderRotation = StartRotation;
            CurrentRenderPosition = StartPosition;
            SetCurrentRotation(StartRotation);
        }

        public void BeginUpdate(Camera camera)
        {
            StartRotation = camera.transform.rotation;
            StartPosition = camera.transform.position;
        }

        public void UpdateRenderTransform(Vector3 position, Quaternion rotation)
        {
            LastRenderPosition = CurrentRenderPosition;
            LastRenderRotation = CurrentRenderRotation;
            CurrentRenderPosition = position;
            SetCurrentRotation(rotation);
        }

        public void UpdateLogicTransform()
        {
            LastLogicPosition = CurrentLogicPosition;
            LastLogicRotation = CurrentLogicRotation;
        }

        public void SetRotation(Quaternion rotation)
        {
            LastLogicRotation = rotation;
            LastRenderRotation = rotation;
            CurrentLogicRotation = rotation;
            SetCurrentRotation(rotation);
        }

        private void SetCurrentRotation(Quaternion rotation)
        {
            CurrentRenderRotation = rotation;
            var euler = rotation.eulerAngles;
            CurrentPitch = euler.x;
            CurrentYaw = euler.y;
        }

        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public Vector3 CurrentLogicPosition;
        public Quaternion CurrentLogicRotation;
        public Vector3 LastLogicPosition;
        public Quaternion LastLogicRotation;
        public Vector3 CurrentRenderPosition;
        public Quaternion CurrentRenderRotation;
        public Vector3 LastRenderPosition;
        public Quaternion LastRenderRotation;
        public float CurrentPitch;
        public float CurrentYaw;
    }
}

//XDay