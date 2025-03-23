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
using XDay.InputAPI;

namespace XDay.CameraAPI
{
    internal partial class BehaviourPinch
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourType Type => BehaviourType.Pinch;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
                m_ZoomFactorUpdater = new(manipulator);
            }

            protected override BehaviourState UpdateInternal(CameraTransform pos)
            {
                return BehaviourState.Over;
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                var req = request as Request;
                if (req.State == MotionState.End)
                {
                    m_CalculatorInitialized = false;
                }
                else
                {
                    if (req.IsRotating)
                    {
                        Rotate(m_Manipulator.FocusPoint, 
                            req.DragDirection, 
                            req.DragDistance, 
                            m_Manipulator.Setup.Orbit, 
                            pos.CurrentLogicPosition, 
                            pos.CurrentLogicRotation, 
                            out pos.CurrentLogicPosition, out pos.CurrentLogicRotation);

                        m_ZoomFactorUpdater.Start();
                        m_CalculatorInitialized = false;
                    }
                    else
                    {
                        if (!m_CalculatorInitialized)
                        {
                            m_CalculatorInitialized = true;
                            m_ZoomFactorUpdater.Start();
                        }

                        var center = req.ZoomCenter;
                        m_ZoomFactorUpdater.AbsoluteZoom(center.x, center.y, req.ZoomRate, ref pos.CurrentLogicPosition);
                    }
                }
            }

            private void Rotate(
               Vector3 focusPoint,
               float dragDir,
               float dragOffset,
               CameraSetup.OrbitSetup orbit,
               Vector3 oldPos,
               Quaternion oldRot,
               out Vector3 newPos,
               out Quaternion newRot)
            {
                var euler = oldRot.eulerAngles;

                var posToFocus = oldPos - focusPoint;
                var radius = posToFocus.magnitude * Mathf.Cos(euler.x * Mathf.Rad2Deg);
                var deltaRot = (dragOffset * 3) / radius * Mathf.Rad2Deg * dragDir;

                var curYRot = euler.y;
                var newYRot = curYRot + deltaRot;
                if (orbit.EnableUnrestrictedOrbit)
                {
                    newRot = Quaternion.Euler(euler.x, newYRot, euler.z);
                    newPos = focusPoint + Quaternion.Euler(0, deltaRot, 0) * posToFocus;
                }
                else
                {
                    var maxAngle = orbit.Range;
                    var clampedDeltaRot = Mathf.Abs(deltaRot);
                    if (newYRot > maxAngle)
                    {
                        clampedDeltaRot = Mathf.Abs(deltaRot) - Mathf.Abs(newYRot - maxAngle);
                    }
                    else if (newYRot < -maxAngle)
                    {
                        clampedDeltaRot = Mathf.Abs(deltaRot) - Mathf.Abs(newYRot + maxAngle);
                    }
                    clampedDeltaRot *= Mathf.Sign(deltaRot);
                    curYRot += clampedDeltaRot;

                    newRot = Quaternion.Euler(euler.x, curYRot, euler.z);
                    newPos = focusPoint + Quaternion.Euler(0, clampedDeltaRot, 0) * posToFocus;
                }
            }

            private ZoomFactorUpdater m_ZoomFactorUpdater;
            private bool m_CalculatorInitialized = false;
        }
    }
}

//XDay
