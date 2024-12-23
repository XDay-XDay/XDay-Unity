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

namespace XDay.CameraAPI
{
    internal partial class BehaviourScrollZoom
    {
        internal class Request : BehaviourRequest
        {
            public float ScrollDelta => m_ScrollDelta;
            public Vector2 TouchPosition => m_TouchPosition;
            public override BehaviourType Type => BehaviourType.ScrollZoom;

            public static Request Create(int layer, int priority, RequestQueueType queueType, Vector2 touchPosition, float scrollDelta)
            {
                var request = m_Pool.Get();
                request.Init(layer, priority, queueType, touchPosition, scrollDelta);
                return request;
            }

            private void Init(int layer, int priority, RequestQueueType method, Vector2 touchPosition, float scrollDelta)
            {
                Init(layer, priority, method);

                m_TouchPosition = touchPosition;
                m_ScrollDelta = scrollDelta;
            }

            public override void OnDestroy(bool overridden)
            {
                m_Pool.Release(this);
            }

            private float m_ScrollDelta;
            private Vector2 m_TouchPosition;
            private static ObjectPool<Request> m_Pool = new(() => { return new Request(); });
        }
    }
}

//XDay