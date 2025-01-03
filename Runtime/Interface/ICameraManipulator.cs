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


using XDay.InputAPI;
using System;
using UnityEngine;

namespace XDay.CameraAPI
{
    public enum RequestQueueType
    {
        Keep,
        Drop,
        Replace,
    }

    public enum FocusMovementType
    {
        Line,
        HorizontalAndVertical,
    }

    public interface IFollowTarget
    {
        bool IsValid { get; }
        Vector3 Position { get; }

        void OnStopFollow();
    }

    public class FocusParam
    {
        public FocusParam(Vector3 focusPoint, float targetAltitude)
        {
            FocusPoint = focusPoint;
            TargetAltitude = targetAltitude;
        }

        public Vector3 FocusPoint;
        public float TargetAltitude;
        public Action ReachTargetCallback = null;
        public FocusMovementType MovementType = FocusMovementType.HorizontalAndVertical;
        public float MoveDuration = 0.5f;
        public float m_ZoomTime = 0.5f;
        public BehaviourMask InterruptMask = BehaviourMask.All;
        public Vector2 ScreenPosition = new Vector2(-1, -1);
        public RequestQueueType QueueType = RequestQueueType.Replace;
        public float ReachDistance = 0.1f;
        public int Layer = 0;
        public int Priority = 0;
        public bool AlwaysInvokeCallback = true;
        public bool OverrideMovement = true;
        public AnimationCurve MoveModulator = null;
        public AnimationCurve ZoomModulator = null;
    }

    public class FollowParam
    {
        public FollowParam(IFollowTarget target)
        {
            Target = target;
        }

        public IFollowTarget Target;
        public float CatchDuration = 0.5f;
        public Action TargetFollowedCallback = null;
        public float Latency = 0;
        public float ZoomDuration = 0;
        public float TargetAltitude = 0;
        public BehaviourMask InterruptionMask = BehaviourMask.All;
        public RequestQueueType QueueType = RequestQueueType.Replace;
        public int Priority = 0;
        public int Layer = 0;
    }

    public interface ICameraManipulator
    {
        static ICameraManipulator Create(Camera camera, CameraSetup setup, IDeviceInput input)
        {
            var manipulator = new CameraManipulator();
            manipulator.Init(setup, camera, input);
            return manipulator;
        }

        event Action<ICameraManipulator> EventPositionChange;
        event Action<ICameraManipulator> EventAltitudeChange;
        public event Action<ICameraManipulator> EventActiveStateChange;

        bool EnableZoom { set; }
        bool EnableDrag { set; }
        bool EnableRestore { get; }
        bool EnableFocusPointClampXZ { get; set; }
        bool IsRenderTransformChanged { get; }
        Camera Camera { get; }
        Vector3 RenderPosition { get; }
        Vector3 FocusPoint { get; }
        float MaxAltitude { get; set; }

        void OnDestroy();

        void SetFocusPointBounds(Vector2 min, Vector2 max);

        void Focus(FocusParam param);

        void SetRestoreEdgeLength(float length);

        void Shake(float duration, float frequency = 10.0f, float range = 0.2f, bool changeAltitude = false);

        void SetPosition(Vector3 position, Action onCameraReachTarget = null, int priority = 0);

        void FollowTarget(FollowParam param);
        void StopFollow();

        void LateUpdate();

        void SetActive(bool active);

        bool IsBehaviourRunning(BehaviourType actionType);
    }
}
