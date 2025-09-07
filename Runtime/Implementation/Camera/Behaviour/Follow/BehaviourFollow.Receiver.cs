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
using System;
using UnityEngine;

namespace XDay.CameraAPI
{
    internal partial class BehaviourFollow
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourMask Interrupter => m_Interrupters;
            public override BehaviourType Type => BehaviourType.Follow;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                Stop();

                if (request is RequestFollow req)
                {
                    Execute(req, pos);
                }
            }

            public override void OnBeingInterrupted(BehaviourRequest request)
            {
                Stop();
            }

            public override void Over()
            {
                Stop();
            }

            private void Execute(RequestFollow request, CameraTransform pose)
            {
                SetState(State.Catch);
                m_Target = request.Param.Target;
                m_MinFollowSpeed = request.Param.MinFollowSpeed;
                m_Interrupters = request.Interrupters;
                m_OnTargetFollowed = request.Param.TargetFollowedCallback;
                m_TargetAltitude = request.Param.TargetAltitude;
                if (m_TargetAltitude == 0)
                {
                    if (m_Manipulator.Direction == CameraDirection.XZ)
                    {
                        m_TargetAltitude = pose.CurrentLogicPosition.y;
                    }
                    else
                    {
                        m_TargetAltitude = -pose.CurrentLogicPosition.z;
                    }
                }
                m_CatchDuration = request.Param.CatchDuration;
                m_Latency = request.Param.Latency;
                m_ZoomDuration = request.Param.ZoomDuration;
            }

            protected override BehaviourState UpdateInternal(CameraTransform pose)
            {
                if (m_State == State.Catch)
                {
                    Catching(pose);
                }

                if (m_State == State.PrepareZoom)
                {
                    PrepareZoom(pose);
                }

                if (m_State == State.Zoom)
                {
                    Zoom(pose);
                }

                if (m_State == State.Followed)
                {
                    Follow(pose);
                }

                return m_State == State.Idle ? BehaviourState.Over : BehaviourState.Running;
            }

            private void Stop()
            {
                if (m_Target != null)
                {
                    m_Target.OnStopFollow();
                    m_Target = null;
                }

                SetState(State.Idle);
                m_CatchSpeed = 0;
                m_CameraAltitudeWhenZoomBegin = 0;
                m_CatchDuration = 0;
                m_Latency = 0;
                m_ZoomTicker.Reset();
                m_LagTicker.Reset();
                m_ZoomDuration = 0;
                m_TargetAltitude = 0;
                m_SpeedModulator = null;
            }

            private void Follow(CameraTransform pose)
            {
                if (!m_Target.IsValid)
                {
                    Stop();
                }
                else
                {
                    if (m_Manipulator.Direction == CameraDirection.XZ)
                    {
                        pose.CurrentLogicPosition = AltitudeToPosition(pose.CurrentLogicPosition.y);
                    }
                    else
                    {
                        pose.CurrentLogicPosition = AltitudeToPosition(-pose.CurrentLogicPosition.z);
                    }
                }
            }

            private void PrepareZoom(CameraTransform pose)
            {
                if (m_LagTicker.Step(Time.deltaTime))
                {
                    SetState(State.Zoom);
                    m_ZoomTicker.Start(m_ZoomDuration);

                    if (m_Manipulator.Direction == CameraDirection.XZ)
                    {
                        m_CameraAltitudeWhenZoomBegin = pose.CurrentRenderPosition.y;
                    }
                    else
                    {
                        m_CameraAltitudeWhenZoomBegin = -pose.CurrentRenderPosition.z;
                    }
                }

                if (m_Manipulator.Direction == CameraDirection.XZ)
                {
                    pose.CurrentLogicPosition = AltitudeToPosition(pose.CurrentLogicPosition.y);
                }
                else
                {
                    pose.CurrentLogicPosition = AltitudeToPosition(-pose.CurrentLogicPosition.z);
                }
            }

            private void Zoom(CameraTransform pose)
            {
                if (m_ZoomTicker.Step(Time.deltaTime))
                {
                    SetState(State.Followed);
                    m_OnTargetFollowed?.Invoke();
                }

                var t = m_ZoomTicker.NormalizedTime;
                if (m_SpeedModulator != null)
                {
                    t = m_SpeedModulator.Evaluate(t);
                }

                pose.CurrentLogicPosition = AltitudeToPosition(Mathf.Lerp(m_CameraAltitudeWhenZoomBegin, m_TargetAltitude, t));
            }

            private void Catching(CameraTransform pos)
            {
                Vector3 cameraPos;
                if (m_Manipulator.Direction == CameraDirection.XZ)
                {
                    cameraPos = AltitudeToPosition(pos.CurrentLogicPosition.y);
                }
                else
                {
                    cameraPos = AltitudeToPosition(-pos.CurrentLogicPosition.z);
                }
                if (m_CatchSpeed == 0)
                {
                    if (m_CatchDuration <= 0)
                    {
                        m_CatchSpeed = float.MaxValue;
                    }
                    else
                    {
                        m_CatchSpeed = Vector3.Distance(cameraPos, pos.CurrentLogicPosition) / m_CatchDuration;
                    }
                }

                m_CatchSpeed = Mathf.Max(m_CatchSpeed, m_MinFollowSpeed);
                pos.CurrentLogicPosition = Vector3.MoveTowards(pos.CurrentLogicPosition, cameraPos, m_CatchSpeed * Time.deltaTime);
                if (pos.CurrentLogicPosition == cameraPos)
                {
                    m_LagTicker.Start(m_Latency);
                    SetState(State.PrepareZoom);
                }
            }

            private Vector3 AltitudeToPosition(float altitude)
            {
                if (m_Manipulator.Direction == CameraDirection.XZ)
                {
                    return Helper.FromFocusPointXZ(m_Manipulator.Camera, m_Target.Position, altitude);
                }
                return Helper.FromFocusPointXY(m_Manipulator.Camera, m_Target.Position, altitude);
            }

            private void SetState(State state)
            {
                m_State = state;
            }

            private enum State
            {
                Idle,
                PrepareZoom,
                Followed,
                Zoom,
                Catch,
            }

            private State m_State;
            private float m_TargetAltitude = 0;
            private IFollowTarget m_Target;
            private float m_Latency;
            private float m_CatchDuration;
            private float m_CameraAltitudeWhenZoomBegin;
            private float m_ZoomDuration;
            private float m_CatchSpeed;
            private AnimationCurve m_SpeedModulator;
            private Action m_OnTargetFollowed;
            private float m_MinFollowSpeed;
            private BehaviourMask m_Interrupters;
            private Ticker m_ZoomTicker = new();
            private Ticker m_LagTicker = new();
        }
    }
}

//XDay