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

using UnityEngine.Pool;

namespace XDay.CameraAPI
{
    internal partial class BehaviourFollow
    {
        internal class RequestStopFollow : BehaviourRequest
        {
            public override BehaviourType Type => BehaviourType.StopFollow;

            public static RequestStopFollow Create(int layer, int priority, RequestQueueType queueType)
            {
                var request = m_Pool.Get();
                request.Init(layer, priority, queueType);
                return request;
            }

            public override void OnDestroy(bool overridden)
            {
                m_Pool.Release(this);
            }

            private static ObjectPool<RequestStopFollow> m_Pool = new(() => { return new RequestStopFollow(); });
        }

        internal class RequestFollow : BehaviourRequest
        {
            public override BehaviourType Type => BehaviourType.Follow;
            public FollowParam Param => m_Param;

            public static RequestFollow Create(FollowParam param)
            {
                var request = m_Pool.Get();
                request.Init(param);
                return request;
            }

            private void Init(FollowParam param)
            {
                Init(param.Layer, param.Priority, param.QueueType);

                m_Param = param;
            }

            public override void OnDestroy(bool overridden)
            {
                m_Pool.Release(this);
            }

            private FollowParam m_Param;
            private static ObjectPool<RequestFollow> m_Pool = new(() => { return new RequestFollow(); });
        }
    }
}

//XDay