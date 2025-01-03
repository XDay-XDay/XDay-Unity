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

using System;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.CameraAPI
{
    internal partial class BehaviourFocus
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourMask Interrupter => m_InterruptMask;
            public override BehaviourType Type => BehaviourType.Focus;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
                m_LineMovement = new(manipulator);
                m_HorizontalAndVerticalMovement = new(manipulator);
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform transform)
            {
                var req = request as Request;

                m_MovementType = req.FocusParam.MovementType;

                if (req.FocusParam.OverrideMovement || 
                    m_CurrentMovement == null)
                {
                    if ( m_CurrentMovement != null &&
                        m_AlwaysInvokeCallback)
                    {
                        m_CameraReachTargetCallback?.Invoke();
                    }

                    m_CameraReachTargetCallback = req.FocusParam.ReachTargetCallback;
                    m_AlwaysInvokeCallback = req.FocusParam.AlwaysInvokeCallback;
                    m_InterruptMask = req.Interrupters;
                    if (m_MovementType == FocusMovementType.HorizontalAndVertical)
                    {
                        m_CurrentMovement = m_HorizontalAndVerticalMovement;
                    }
                    else if (m_MovementType == FocusMovementType.Line)
                    {
                        m_CurrentMovement = m_LineMovement;
                    }
                    else
                    {
                        Debug.Assert(false, "Todo");
                    }

                    CalculateOffset(req);

                    m_CurrentMovement.Respond(req, transform);
                }
            }

            protected override BehaviourState UpdateInternal(CameraTransform pose)
            {
                return m_CurrentMovement.Update(pose);
            }

            public override void OnBeingInterrupted(BehaviourRequest request)
            {
                if (request is not Request)
                {
                    OverInternal(false);
                }
            }

            public override void Over()
            {
                OverInternal(true);
            }

            private void OverInternal(bool triggerCallback)
            {
                m_CurrentMovement.Over();
                m_CurrentMovement = null;

                if (IsActive &&
                    triggerCallback)
                {
                    m_CameraReachTargetCallback?.Invoke();
                }
            }

            private void CalculateOffset(Request req)
            {
                var param = req.FocusParam;
                if (param.ScreenPosition.x >= 0 &&
                    param.ScreenPosition.y >= 0)
                {
                    var camera = m_Manipulator.Camera;
                    var oldPos = camera.transform.position;
                    camera.transform.position = Helper.FromFocusPoint(camera, param.FocusPoint, param.TargetAltitude);
                    var screenCenter = Helper.RayCastWithXZPlane(new Vector3(camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f, 0), camera, param.FocusPoint.y);
                    var screenPos = Helper.RayCastWithXZPlane(new Vector3(param.ScreenPosition.x, param.ScreenPosition.y, 0), camera, param.FocusPoint.y);
                    camera.transform.position = oldPos;
                    param.FocusPoint += screenCenter - screenPos;
                }
                
            }

            private bool m_AlwaysInvokeCallback;
            private Action m_CameraReachTargetCallback;
            private LineMovement m_LineMovement;
            private HorizontalAndVertical m_HorizontalAndVerticalMovement;
            private FocusMovementType m_MovementType = FocusMovementType.Line;
            private BehaviourMask m_InterruptMask;
            private Movement m_CurrentMovement;
        }
    }
}

//XDay