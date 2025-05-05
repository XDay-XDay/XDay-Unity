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
using XDay.UtilityAPI;
using UnityEngine;

namespace XDay.CameraAPI
{
    internal partial class BehaviourDrag
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourType Type => BehaviourType.Drag;
            public float RestoreMinSpeed
            {
                get => m_BounceMinSpeed;
                set => m_BounceMinSpeed = value;
            }
            public float RestoreDuration
            {
                get => m_BounceTime;
                set => m_BounceTime = value;
            }

            public Receiver(CameraManipulator manipulator, float deaccelerateTime, float inertialTime)
                : base(manipulator)
            {
                m_InertialTime = inertialTime;
                m_DeaccelerateTime = deaccelerateTime;
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                var req = request as Request;

                if (m_State != State.Bounce)
                {
                    if (req.State == MotionState.Start)
                    {
                        SetState(State.Dragging);
                        m_InertialStartDirection = Vector3.zero;
                    }
                    else if (req.State == MotionState.Running)
                    {
                        SetState(State.Dragging);

                        pos.CurrentLogicPosition += req.DragOffset;

                        m_Manipulator.EnableRestore = true;
                    }
                    else if (req.State == MotionState.End)
                    {
                        BounceCheck(req, pos);
                    }
                }
            }

            protected override BehaviourState UpdateInternal(CameraTransform pos)
            {
                var over = false;
                if (m_State == State.Bounce)
                {
                    pos.CurrentLogicPosition = Bounce();
                }
                else if (m_State == State.Deacclerate)
                {
                    pos.CurrentLogicPosition = Deaccelerate(pos.CurrentLogicPosition, out over);
                }
                else if (m_State == State.InertialState)
                {
                    pos.CurrentLogicPosition = InertialMove(pos.CurrentLogicPosition);
                }
                else
                {
                    over = true;
                }

                return over ? BehaviourState.Over : BehaviourState.Running;
            }

            public override void OnBeingInterrupted(BehaviourRequest request)
            {
                Over();
            }

            public override void Over()
            {
                m_Timer = 0;
                m_InertialStartDirection = Vector3.zero;
                SetState(State.Idle);
            }

            private Vector3 Bounce()
            {
                if (m_BounceEndPos == m_BounceStartPos)
                {
                    SetState(State.Idle);

                    m_Manipulator.EnableZoom = true;
                    m_Manipulator.EnableRestore = false;
                }

                m_BounceStartPos = Vector3.MoveTowards(m_BounceStartPos, m_BounceEndPos, m_BounceSpeed * Time.deltaTime);

                return m_BounceStartPos;
            }

            private bool CheckBounceNeeded(Vector3 cameraPos)
            {
                var bounceNeeded = false;
                if (m_Manipulator.EnableFocusPointClampXZ)
                {
                    var areaMin = m_Manipulator.AreaMin;
                    var areaMax = m_Manipulator.AreaMax;
                    var focusPoint = m_Manipulator.FocusPoint;

                    var direction = m_Manipulator.Setup.Direction;
                    bool outOfRange = CheckRange(direction, focusPoint, areaMin, areaMax);
                    if (outOfRange)
                    {
                        m_Manipulator.EnableZoom = false;

                        SetState(State.Bounce);

                        Vector3 intersection = Vector3.zero;
                        if (direction == CameraDirection.XZ)
                        {
                            intersection = Helper.ClampPointInXZPlane(focusPoint, areaMin, areaMax);
                            m_BounceEndPos = Helper.FromFocusPointXZ(m_Manipulator.Camera, intersection, cameraPos.y);
                        }
                        else if (direction == CameraDirection.XY)
                        {
                            intersection = Helper.ClampPointInXYPlane(focusPoint, areaMin, areaMax);
                            m_BounceEndPos = Helper.FromFocusPointXY(m_Manipulator.Camera, intersection, -cameraPos.z);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                        
                        m_BounceStartPos = cameraPos;
                        m_BounceSpeed = Mathf.Max(m_BounceMinSpeed, Vector3.Distance(m_BounceEndPos, m_BounceStartPos) / m_BounceTime);

                        bounceNeeded = true;
                    }
                    else
                    {
                        m_Manipulator.EnableRestore = false;
                    }
                }

                return bounceNeeded;
            }

            private Vector3 Deaccelerate(Vector3 cameraPos, out bool over)
            {
                var dt = Time.deltaTime;
                m_Timer += dt;
                over = false;
                if (m_Timer >= m_DeaccelerateTime)
                {
                    m_Timer = m_DeaccelerateTime;
                    over = true;
                }

                var velocity = m_InertialStartDirection * Helper.EaseOutQuad(m_Deacceleration, 0, Mathf.Clamp01(m_Timer / m_DeaccelerateTime));
                cameraPos = cameraPos + velocity * dt;

                if (over)
                {
                    Over();
                }

                return cameraPos;
            }

            private Vector3 InertialMove(Vector3 newCameraPos)
            {
                m_Timer += Time.deltaTime;
                var over = false;
                if (m_Timer >= m_InertialTime)
                {
                    m_Timer = m_InertialTime;
                    over = true;
                }

                var t = Mathf.Clamp01(m_Timer / m_InertialTime);
                var velocity = m_InertialStartDirection * Mathf.Lerp(m_InertialStartSpeed, m_Deacceleration, Helper.EaseOutExp(0, 1, t));
                newCameraPos = newCameraPos + velocity * Time.deltaTime;
                if (over)
                {
                    m_Timer = 0;
                    SetState(State.Deacclerate);
                }
                return newCameraPos;
            }

            private void SetState(State state)
            {
                m_State = state;
            }

            private void BounceCheck(Request req, CameraTransform pos)
            {
                var bounce = CheckBounceNeeded(pos.CurrentLogicPosition);
                if (!bounce)
                {
                    if (req.TouchCount > 1 &&
                        req.TouchTime > 0)
                    {
                        SetState(State.InertialState);
                        m_Timer = 0;
                        m_InertialStartDirection = req.DragDirection;
                        m_InertialStartSpeed = req.TouchDistance / req.TouchTime;
                        m_Deacceleration = m_InertialStartSpeed * 0.01f;
                    }
                    else
                    {
                        Over();
                    }
                }
            }

            private bool CheckRange(CameraDirection direction, Vector3 focusPoint, Vector2 areaMin, Vector2 areaMax)
            {
                if (direction == CameraDirection.XZ)
                {
                    return focusPoint.x < areaMin.x ||
                            focusPoint.z < areaMin.y ||
                            focusPoint.x > areaMax.x ||
                            focusPoint.z > areaMax.y;
                }
                else if (direction == CameraDirection.XY)
                {
                    return focusPoint.x < areaMin.x ||
                            focusPoint.y < areaMin.y ||
                            focusPoint.x > areaMax.x ||
                            focusPoint.y > areaMax.y;
                }
                Debug.Assert(false, "todo");
                return false;
            }

            private enum State
            {
                Idle,
                Dragging,
                InertialState,
                Deacclerate,
                Bounce,
            }

            private float m_Timer;
            private float m_DeaccelerateTime;
            private float m_Deacceleration;
            private Vector3 m_BounceStartPos;
            private Vector3 m_BounceEndPos;
            private float m_BounceMinSpeed = 50;
            private float m_BounceTime = 0.3f;
            private float m_BounceSpeed;
            private float m_InertialStartSpeed;
            private float m_InertialTime;
            private Vector3 m_InertialStartDirection;
            private State m_State = State.Idle;
        }
    }
}


//XDay