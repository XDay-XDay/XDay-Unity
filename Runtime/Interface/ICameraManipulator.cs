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
        /// <summary>
        /// move in a line
        /// </summary>
        Line,

        /// <summary>
        /// first move horizontally then move vertically
        /// </summary>
        HorizontalAndVertical,
    }

    public interface IFollowTarget
    {
        /// <summary>
        /// is valid target
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// follow target's position
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// called when follow stopped
        /// </summary>
        void OnStopFollow();
    }

    /// <summary>
    /// camera focus parameter
    /// </summary>
    public class FocusParam
    {
        public FocusParam(Vector3 focusPoint, float targetAltitude)
        {
            Debug.Assert(targetAltitude > 0, $"无效的目标高度{targetAltitude},相机目标高度要大于0");

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

    /// <summary>
    /// manipulate camera movement
    /// </summary>
    public interface ICameraManipulator
    {
        /// <summary>
        /// create camera manipulator instance
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="setup">camera setting</param>
        /// <param name="input"></param>
        /// <returns></returns>
        static ICameraManipulator Create(Camera camera, CameraSetup setup, IDeviceInput input)
        {
            var manipulator = new CameraManipulator();
            manipulator.Init(setup, camera, input);
            return manipulator;
        }

        /// <summary>
        /// triggered when camera position changes
        /// </summary>
        event Action<ICameraManipulator> EventPositionChange;

        /// <summary>
        /// triggered when camera height changes
        /// </summary>
        event Action<ICameraManipulator> EventAltitudeChange;

        /// <summary>
        /// triggered when camera manipulator is enabled/disabled
        /// </summary>
        event Action<ICameraManipulator> EventActiveStateChange;

        /// <summary>
        /// enable camera zooming or not
        /// </summary>
        bool EnableZoom { set; }
        /// <summary>
        /// enable camera dragging or not
        /// </summary>
        bool EnableDrag { set; }
        /// <summary>
        /// enable camera bounce back when out of bounds or not
        /// </summary>
        bool EnableRestore { get; set; }
        /// <summary>
        /// enable camera position clamp in xz plane or not
        /// </summary>
        bool EnableFocusPointClamp { get; set; }
        /// <summary>
        /// is camera transform changed
        /// </summary>
        bool IsRenderTransformChanged { get; }
        /// <summary>
        /// get camera instance
        /// </summary>
        Camera Camera { get; }
        /// <summary>
        /// camera current position
        /// </summary>
        Vector3 RenderPosition { get; }
        /// <summary>
        /// camera focus point
        /// </summary>
        Vector3 FocusPoint { get; }
        /// <summary>
        /// set/get camera max height
        /// </summary>
        float MaxAltitude { get; set; }

        Vector3 Forward { get; }

        CameraDirection Direction { get; }

        void OnDestroy();

        /// <summary>
        /// limit camera horizontal movement range
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetFocusPointBounds(Vector2 min, Vector2 max);

        /// <summary>
        /// set camera focus point
        /// </summary>
        /// <param name="param"></param>
        void Focus(FocusParam param);

        /// <summary>
        /// how far can camera move out of bounds
        /// </summary>
        /// <param name="distance"></param>
        void SetPenetrationDistance(float distance);

        /// <summary>
        /// shake camera
        /// </summary>
        /// <param name="duration">how long shake lasts</param>
        /// <param name="frequency">how fast</param>
        /// <param name="range">how far</param>
        /// <param name="changeAltitude">if shake can affect camera position.y</param>
        void Shake(float duration, float frequency = 10.0f, float range = 0.2f, bool changeAltitude = false);

        /// <summary>
        /// set camera position immediately
        /// </summary>
        /// <param name="position"></param>
        /// <param name="onCameraReachTarget"></param>
        /// <param name="priority"></param>
        void SetPosition(Vector3 position, Action onCameraReachTarget = null);

        /// <summary>
        /// camera follows target
        /// </summary>
        /// <param name="param"></param>
        void FollowTarget(FollowParam param);

        /// <summary>
        /// stop camera follow
        /// </summary>
        void StopFollow();

        void LateUpdate();

        /// <summary>
        /// enable/disable camera manipulator
        /// </summary>
        /// <param name="active"></param>
        void SetActive(bool active);

        /// <summary>
        /// is camera running specified behaviour
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsBehaviourRunning(BehaviourType type);
    }
}
