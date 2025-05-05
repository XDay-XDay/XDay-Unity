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
    internal partial class BehaviourFocus
    {
        internal class HorizontalAndVertical : Movement
        {
            public HorizontalAndVertical(CameraManipulator manipulator)
                : base(manipulator)
            {
                m_Calculator = new(manipulator);
                m_Direction = manipulator.Direction;
            }

            public override void Over()
            {
                SetState(State.Idle);
            }

            public override void Respond(Request request, CameraTransform pos)
            {
                m_Timer = 0;
                m_StartPos = pos.StartPosition;
                m_MoveTime = request.FocusParam.MoveDuration;
                if (m_Direction == CameraDirection.XZ)
                {
                    m_EndPos = Helper.FromFocusPointXZ(m_Manipulator.Camera, request.FocusParam.FocusPoint, request.FocusParam.TargetAltitude);
                }
                else
                {
                    m_EndPos = Helper.FromFocusPointXY(m_Manipulator.Camera, request.FocusParam.FocusPoint, request.FocusParam.TargetAltitude);
                }
                m_ScaleCurve = request.FocusParam.ZoomModulator;
                m_MoveCurve = request.FocusParam.MoveModulator;
                m_ReachDistance = request.FocusParam.ReachDistance;
                SetState(State.Move);

                if (m_Direction == CameraDirection.XZ)
                {
                    Begin(request.FocusParam.m_ZoomTime, request.FocusParam.FocusPoint.x, request.FocusParam.FocusPoint.z, m_Manipulator.Camera, pos);
                }
                else
                {
                    Begin(request.FocusParam.m_ZoomTime, request.FocusParam.FocusPoint.x, request.FocusParam.FocusPoint.y, m_Manipulator.Camera, pos);
                }
            }

            public override BehaviourState Update(CameraTransform pos)
            {
                if (m_StartPos == m_EndPos)
                {
                    pos.CurrentLogicPosition = m_EndPos;
                    return BehaviourState.Over;
                }

                m_Timer += Time.deltaTime;
                return Move(pos);
            }

            private void Begin(float zoomTime, float focusX, float focusZ, Camera camera, CameraTransform pose)
            {
                if (!CheckZoomNeeded(m_StartPos, m_EndPos))
                {
                    m_ZoomTime = 0;
                    m_MoveEndPos = m_EndPos;
                }
                else
                {
                    if (m_Direction == CameraDirection.XZ)
                    {
                        m_MoveEndPos = CalculateMoveEndPosXZ(focusX, focusZ, m_StartPos.y, camera.transform.forward, pose.CurrentLogicRotation);
                    }
                    else
                    {
                        m_MoveEndPos = CalculateMoveEndPosXY(focusX, focusZ, -m_StartPos.z, camera.transform.forward, pose.CurrentLogicRotation);
                    }
                    m_ZoomTime = zoomTime;
                }

                m_Calculator.Reset();

                if (Vector3.Distance(m_MoveEndPos, m_StartPos) <= m_ReachDistance)
                {
                    SetState(State.Zoom);
                }
                else if (Vector3.Distance(m_EndPos, m_StartPos) <= m_ReachDistance)
                {
                    SetState(State.ReachTarget);
                }
            }

            private BehaviourState Move(CameraTransform pose)
            {
                var result = BehaviourState.Running;
                if (m_State == State.ReachTarget)
                {
                    pose.CurrentLogicPosition = m_EndPos;
                    result = BehaviourState.Over;
                }
                else if (m_State == State.Move)
                {
                    var t = 1.0f;
                    if (!Mathf.Approximately(m_MoveTime, 0))
                    {
                        t = Mathf.Clamp(m_Timer, 0, m_MoveTime) / m_MoveTime;
                        if (m_MoveCurve != null)
                        {
                            t = Mathf.Clamp01(m_MoveCurve.Evaluate(t));
                        }
                    }

                    var x = Mathf.SmoothStep(m_StartPos.x, m_MoveEndPos.x, t);
                    var y = Mathf.SmoothStep(m_StartPos.y, m_MoveEndPos.y, t);
                    var z = Mathf.SmoothStep(m_StartPos.z, m_MoveEndPos.z, t);

                    if (m_Timer >= m_MoveTime)
                    {
                        if (!CheckZoomNeeded(m_StartPos, m_EndPos))
                        {
                            result = BehaviourState.Over;
                        }
                        else
                        {
                            SetState(State.Zoom);
                        }
                    }

                    pose.CurrentLogicPosition = new Vector3(x, y, z);
                }
                else if (m_State == State.Zoom)
                {
                    var finish = m_Calculator.Update(m_ScaleCurve, ref pose.CurrentLogicPosition);
                    if (finish)
                    {
                        result = BehaviourState.Over;
                    }
                }

                return result;
            }

            private Vector3 CalculateMoveEndPosXZ(float focusX, float focusZ, float startY, Vector3 forward, Quaternion rotation)
            {
                var distance = Helper.FocalLengthFromAltitudeXZ(rotation.eulerAngles.x, startY);
                return new Vector3(focusX, 0, focusZ) - forward * distance;
            }

            private Vector3 CalculateMoveEndPosXY(float focusX, float focusZ, float startZ, Vector3 forward, Quaternion rotation)
            {
                var distance = Helper.FocalLengthFromAltitudeXY(rotation.eulerAngles.x, startZ);
                return new Vector3(focusX, focusZ, 0) - forward * distance;
            }

            private bool CheckZoomNeeded(Vector3 startPos, Vector3 targetPos)
            {
                if (m_Direction == CameraDirection.XZ)
                {
                    if (Mathf.Approximately(targetPos.y, startPos.y))
                    {
                        return false;
                    }
                }
                if (Mathf.Approximately(targetPos.z, startPos.z))
                {
                    return false;
                }
                return true;
            }

            private void SetState(State state)
            {
                m_State = state;
                if (state == State.Zoom)
                {
                    m_StartPos = m_MoveEndPos;
                    m_MoveEndPos = m_EndPos;
                    m_MoveTime = m_ZoomTime;
                    m_Timer = 0;
                    float zoomFactor;
                    if (m_Direction == CameraDirection.XZ)
                    {
                        zoomFactor = m_Manipulator.Setup.ZoomFactorAtAltitude(m_EndPos.y);
                    }
                    else
                    {
                        zoomFactor = m_Manipulator.Setup.ZoomFactorAtAltitude(-m_EndPos.z);
                    }
                    m_Calculator.Start(zoomFactor, m_ZoomTime);
                }
            }

            private enum State
            {
                Idle,
                Zoom,
                ReachTarget,
                Move,
            }

            private State m_State = State.Idle;
            private float m_ReachDistance;
            private Vector3 m_MoveEndPos;
            private AnimationCurve m_ScaleCurve;
            private AnimationCurve m_MoveCurve;
            private AutoZoomFactorUpdater m_Calculator;
            private float m_Timer;
            private float m_MoveTime;
            private float m_ZoomTime;
            private Vector3 m_StartPos;
            private Vector3 m_EndPos;
            private CameraDirection m_Direction;
        }
    }
}


//XDay