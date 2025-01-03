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
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.CameraAPI
{
    internal partial class BehaviourDrag
    {
        internal class Request : BehaviourRequest
        {
            public int TouchCount => m_TouchCount;
            public MotionState State => m_State;
            public Vector3 DragOffset => m_DragOffset;
            public float TouchTime => m_TouchTime;
            public float TouchDistance => m_TouchDistance;
            public Vector3 DragDirection => m_DragDirection;
            public override BehaviourType Type => BehaviourType.Drag;

            public static Request Create(int layer, int priority, MotionState state, RequestQueueType queueType, int touchCount, Vector3 dragOffset, float touchTime, float touchDistance, Vector3 dragDirection)
            {
                var request = m_Pool.Get();
                request.Init(layer, priority, state, queueType, touchCount, dragOffset, touchTime, touchDistance, dragDirection);
                return request;
            }

            private void Init(int layer, int priority, MotionState state, RequestQueueType queueType, int touchCount, Vector3 dragOffset, float touchTime, float touchDistance, Vector3 dragDirection)
            {
                Init(layer, priority, queueType);

                m_State = state;
                m_DragOffset = dragOffset;
                m_TouchTime = touchTime;
                m_TouchDistance = touchDistance;
                m_DragDirection = dragDirection;
                m_TouchCount = touchCount;
            }

            public override void OnDestroy(bool overridden)
            {
                m_Pool.Release(this);
            }

            private MotionState m_State;
            private Vector3 m_DragOffset;
            private Vector3 m_DragDirection;
            private float m_TouchTime;
            private float m_TouchDistance;
            private int m_TouchCount;
            private static ObjectPool<Request> m_Pool = new(() => { return new Request(); });
        }
    }
}

//XDay