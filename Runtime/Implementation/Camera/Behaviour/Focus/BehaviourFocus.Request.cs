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

using UnityEngine.Pool;

namespace XDay.CameraAPI
{
    internal partial class BehaviourFocus
    {
        internal class Request : BehaviourRequest
        {
            public override BehaviourType Type => BehaviourType.Focus;
            public FocusParam FocusParam => m_Param;

            public static Request Create(FocusParam param)
            {
                Request request = m_Pool.Get();
                request.Init(param);

                return request;
            }

            private void Init(FocusParam param)
            {
                Init(param.Layer, param.Priority, param.QueueType);

                m_Param = param;
                Interrupters = param.InterruptMask;
            }

            public override void OnDestroy(bool overridden)
            {
                if (overridden && m_Param.AlwaysInvokeCallback)
                {
                    m_Param.ReachTargetCallback?.Invoke();
                }
                m_Pool.Release(this);
            }

            private FocusParam m_Param;
            private static ObjectPool<Request> m_Pool = new(() => { return new Request(); });
        }
    }
}

//XDay