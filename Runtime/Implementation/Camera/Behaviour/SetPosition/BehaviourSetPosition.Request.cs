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
using UnityEngine.Pool;

namespace XDay.CameraAPI
{
    internal partial class BehaviourSetPosition
    {
        internal class Request : BehaviourRequest
        {
            public Vector3 Position => m_Position;
            public bool AlwaysInvokeCallback => m_AlwaysInvokeCallback;
            public Action CameraReachTargetCallback => m_OnCameraReachTarget;
            public bool OverrideMovement => m_OverrideMovement;
            public override BehaviourType Type => BehaviourType.SetPosition;

            public static Request Create(int layer, int priority, RequestQueueType queueType, Vector3 position, bool overrideMovement, BehaviourMask interrupters, Action onCameraReachTarget, bool alwaysInvokeCallback)
            {
                var request = m_Pool.Get();
                request.Init(layer, priority, queueType, position, overrideMovement, interrupters, onCameraReachTarget, alwaysInvokeCallback);
                return request;
            }

            private void Init(int layer, int priority, RequestQueueType queueType, Vector3 position, bool overrideMovement, BehaviourMask interrupters, Action onCameraReachTarget, bool alwaysInvokeCallback)
            {
                Init(layer, priority, queueType);

                m_AlwaysInvokeCallback = alwaysInvokeCallback;
                m_OnCameraReachTarget = onCameraReachTarget;
                m_OverrideMovement = overrideMovement;
                Interrupters = interrupters;
                m_Position = position;
            }

            public override void OnDestroy(bool overridden)
            {
                if (m_AlwaysInvokeCallback && overridden)
                {
                    m_OnCameraReachTarget?.Invoke();
                }
                m_Pool.Release(this);
            }

            private Vector3 m_Position;
            private Action m_OnCameraReachTarget;
            private bool m_AlwaysInvokeCallback;
            private bool m_OverrideMovement;
            private static ObjectPool<Request> m_Pool = new(() => { return new Request(); });
        }

    }
}


//XDay