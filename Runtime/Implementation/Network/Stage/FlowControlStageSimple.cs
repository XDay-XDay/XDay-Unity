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

using System.Collections.Generic;
using System.Diagnostics;

namespace XDay.NetworkAPI
{
    /// <summary>
    /// limit flow control per cycle
    /// </summary>
    internal class FlowControlStageSimple : IFlowControlStage
    {
        public FlowControlStageSimple(ITransportable transporter, INetworkContext context)
        {
            m_Transporter = transporter;
            m_Context = context as NetworkContext;
        }

        public void OnDestroy()
        {
            while (m_Cache.Count > 0)
            {
                m_Context.ReleaseByteStream(m_Cache.Dequeue());
            }
        }

        public bool CheckOverflow(ByteStream stream)
        {
            m_Cycle.Step();

            if (m_Cache.Count > 0 || 
                m_Cycle.Overflow)
            {
                m_Cache.Enqueue(stream);
                return true;
            }

            return false;
        }

        public void Update()
        {
            if (m_Cycle.Timeout)
            {
                while (m_Cache.Count > 0)
                {
                    var stream = m_Cache.Peek();
                    m_Cycle.Step();
                    if (!m_Cycle.Overflow)
                    {
                        m_Cache.Dequeue();
                        m_Transporter.Send(stream);
                    }
                    else
                    {
                        //can't send now
                        break;
                    }
                }
            }
        }

        private readonly NetworkContext m_Context;
        private readonly ITransportable m_Transporter;
        private readonly Queue<ByteStream> m_Cache = new(200);
        private readonly Cycle m_Cycle = new();

        private class Cycle
        {
            public bool Timeout => m_Timer.ElapsedMilliseconds >= CycleDurationInMs;
            public bool Overflow => m_Overflow;

            public void Step()
            {
                if (!m_Timer.IsRunning)
                {
                    Start();
                }

                ++m_SentCount;

                var elpasedMilliseconds = m_Timer.ElapsedMilliseconds;
                if (elpasedMilliseconds < CycleDurationInMs)
                {
                    if (m_SentCount > MaxSendCountPerCycle)
                    {
                        m_Overflow = true;
                        UnityEngine.Debug.LogError($"overflow");
                    }
                }
                else
                {
                    Start();
                }
            }

            private void Start()
            {
                m_Timer.Restart();
                m_SentCount = 0;
                m_Overflow = false;
            }

            private readonly Stopwatch m_Timer = new();
            private int m_SentCount;
            private bool m_Overflow;
            private const int MaxSendCountPerCycle = 200;
            private const long CycleDurationInMs = 10 * 1000;
        }
    }

    public class FlowControlStageSimpleCreator : IFlowControlStageCreator
    {
        public IFlowControlStage Create(ITransportable transportable, INetworkContext context)
        {
            return new FlowControlStageSimple(transportable, context);
        }
    }
}

//XDay