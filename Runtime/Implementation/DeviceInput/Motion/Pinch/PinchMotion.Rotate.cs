/*
 * Copyright (c) 2024 XDay
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

namespace XDay.InputAPI
{
    internal partial class PinchMotion
    {
        class Rotate : Phase
        {
            public float SlideDirection => m_SlideDirection;
            public float DraggedOffset => m_DragDistance;

            public Rotate(PinchMotion motion)
                : base(motion) 
            {
            }

            public override void Reset()
            {
                OnDeactivate();
            }

            public override void OnDeactivate()
            {
                m_SlideDirection = 0; 
                m_DragDistance = 0;
            }

            public override bool Match(ITouch touch0, ITouch touch1)
            {
                var camera = m_Motion.Camera;
                var cameraPos = camera.transform.position;

                m_DragDistance = 0;

                var match = false;
   
                var isFinished = (touch0.State == TouchState.Finish ||  touch1.State == TouchState.Finish);

                if (m_Motion.EnableRotate &&
                    m_Motion.Range > 0 &&
                    !isFinished &&
                    cameraPos.y > m_Motion.MinAltitude && 
                    cameraPos.y < m_Motion.MaxAltitude) 
                {
                    if (IsRotating(touch0, touch1))
                    {
                        match = true;

                        var prevPos0 = Helper.RayCastWithXZPlane(touch0.Previous, camera);
                        var prevPos1 = Helper.RayCastWithXZPlane(touch1.Previous, camera);
                        var curPos0 = Helper.RayCastWithXZPlane(touch0.Current, camera);
                        var curPos1 = Helper.RayCastWithXZPlane(touch1.Current, camera);

                        m_DragDistance = Mathf.Max(
                            (curPos0 - prevPos0).magnitude, 
                            (curPos1 - prevPos1).magnitude);
                    }
                }

                m_Motion.CheckPhaseChange(touch0, touch1, (touch0.Current - touch1.Current).normalized);

                return match;
            }

            private bool IsRotating(ITouch touch0, ITouch touch1)
            {
                var isRotating = false;

                var pos0 = touch0.Current;
                var pos1 = touch1.Current;
                var dir0 = pos0 - touch0.Previous;
                var dir1 = pos1 - touch1.Previous;

                Vector2 topMove, bottomMove, leftMove, rightMove;

                var moveX = Mathf.Abs(dir0.x) + Mathf.Abs(dir1.x);
                var moveY = Mathf.Abs(dir0.y) + Mathf.Abs(dir1.y);

                dir0.Normalize();
                dir1.Normalize();

                if (pos0.y > pos1.y)
                {
                    bottomMove = dir1;
                    topMove = dir0;
                }
                else
                {
                    bottomMove = dir0;
                    topMove = dir1;
                }

                if (pos0.x > pos1.x)
                {
                    leftMove = dir1;
                    rightMove = dir0;
                }
                else
                {
                    leftMove = dir0;
                    rightMove = dir1;
                }

                if (moveX <= moveY)
                {
                    if (leftMove.y <= 0 && rightMove.y >= 0)
                    {
                        isRotating = true;
                        m_SlideDirection = 1.0f;
                    }
                    else if (rightMove.y <= 0 && leftMove.y >= 0)
                    {
                        isRotating = true;
                        m_SlideDirection = -1.0f;
                    }
                }
                else
                {
                    if (topMove.x <= 0 && bottomMove.x >= 0)
                    {
                        isRotating = true;
                        m_SlideDirection = 1.0f;
                    }
                    else if (topMove.x >= 0 && bottomMove.x <= 0)
                    {
                        isRotating = true;
                        m_SlideDirection = -1.0f;
                    }
                }

                return isRotating;
            }

            private float m_DragDistance = 0;
            private float m_SlideDirection = 1.0f;
        }
    }
}

//XDay