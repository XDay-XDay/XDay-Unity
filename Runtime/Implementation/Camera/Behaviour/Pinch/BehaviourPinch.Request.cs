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



using UnityEngine;
using UnityEngine.Pool;
using XDay.InputAPI;

namespace XDay.CameraAPI
{
    internal partial class BehaviourPinch
    {
        internal class Request : BehaviourRequest
        {
            public float DragDirection => m_DragDirection;
            public Vector2 ZoomCenter => m_ZoomCenter;
            public bool IsRotating => m_IsRotating;
            public float ZoomRate => m_ZoomRate;
            public float DragDistance => m_DragDistance;
            public MotionState State => m_State;
            public override BehaviourType Type => BehaviourType.Pinch;

            public static Request Create(int layer, int priority, RequestQueueType queueType, Vector2 zoomCenter, float zoomRate, MotionState state, bool isRotating, float dragDistance, float dragDirection)
            {
                var request = m_Pool.Get();
                request.Init(layer, priority, queueType, zoomCenter, zoomRate, state, isRotating, dragDistance, dragDirection);
                return request;
            }

            private void Init(int layer, int priority, RequestQueueType queueType, Vector2 zoomCenter, float zoomRate, MotionState state, bool isRotating, float dragDistance, float dragDirection)
            {
                Init(layer, priority, queueType);

                m_ZoomCenter = zoomCenter;
                m_ZoomRate = zoomRate;
                m_State = state;
                m_IsRotating = isRotating;
                m_DragDistance = dragDistance;
                m_DragDirection = dragDirection;
            }

            public override void OnDestroy(bool overridden)
            {
                m_Pool.Release(this);
            }

            private Vector2 m_ZoomCenter;
            private float m_ZoomRate;
            private MotionState m_State;
            private bool m_IsRotating;
            private float m_DragDistance;
            private float m_DragDirection;
            private static ObjectPool<Request> m_Pool = new(() => { return new Request(); });
        }
    }
}

//XDay