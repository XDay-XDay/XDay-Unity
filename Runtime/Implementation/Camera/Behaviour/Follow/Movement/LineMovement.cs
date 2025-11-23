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
using System;

namespace XDay.CameraAPI
{
    internal partial class BehaviourFollow
    {
        internal class LineMovement : Movement
        {
            public LineMovement(CameraManipulator manipulator)
                : base(manipulator)
            {
            }

            public override void Over()
            {
                m_Target?.OnStopFollow();
                m_Target = null;

                SetState(State.Idle);
                m_CatchSpeed = 0;
                m_CatchDuration = 0;
                m_TargetAltitude = 0;
            }

            public override void Respond(IFollowTarget target, RequestFollow request, CameraTransform transform)
            {
                m_Target = target;
                SetState(State.Catch);
                m_MinFollowSpeed = request.Param.MinFollowSpeed;
                m_OnTargetFollowed = request.Param.TargetFollowedCallback;
                m_TargetAltitude = request.Param.TargetAltitude;
                if (m_TargetAltitude == 0)
                {
                    if (m_Manipulator.Direction == CameraDirection.XZ)
                    {
                        m_TargetAltitude = transform.CurrentLogicPosition.y;
                    }
                    else
                    {
                        m_TargetAltitude = -transform.CurrentLogicPosition.z;
                    }
                }
                m_CatchDuration = request.Param.CatchDuration;
            }

            public override BehaviourState Update(CameraTransform transform)
            {
                if (m_State == State.Catch)
                {
                    Catching(transform);
                }

                if (m_State == State.Followed)
                {
                    Follow(transform);
                }

                return m_State == State.Idle ? BehaviourState.Over : BehaviourState.Running;
            }

            private void Follow(CameraTransform pose)
            {
                if (!m_Target.IsValid)
                {
                    Over();
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

            private void Catching(CameraTransform pos)
            {
                Vector3 targetPos = AltitudeToPosition(m_TargetAltitude);
                if (m_CatchSpeed == 0)
                {
                    if (m_CatchDuration <= 0)
                    {
                        m_CatchSpeed = float.MaxValue;
                    }
                    else
                    {
                        m_CatchSpeed = Vector3.Distance(targetPos, pos.CurrentLogicPosition) / m_CatchDuration;
                    }
                }

                m_CatchSpeed = Mathf.Max(m_CatchSpeed, m_MinFollowSpeed);
                pos.CurrentLogicPosition = Vector3.MoveTowards(pos.CurrentLogicPosition, targetPos, m_CatchSpeed * Time.deltaTime);
                if (pos.CurrentLogicPosition == targetPos)
                {
                    SetState(State.Followed);
                    m_OnTargetFollowed?.Invoke();
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
                Catch,
                Followed,
            }

            private State m_State;
            private float m_CatchDuration;
            private float m_CatchSpeed;
            private Action m_OnTargetFollowed;
            private float m_MinFollowSpeed;
            private IFollowTarget m_Target;
            private float m_TargetAltitude = 0;
        }
    }
}


//XDay