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
using UnityEngine.Pool;
using XDay.UtilityAPI;

namespace XDay.NetworkAPI
{
    internal class QualityStageSimple : IQualityStage
    {
        public QualityStageSimple(
            string name, 
            ITransportable transporter,
            int RTTMin,
            int RTTMax,
            float packetLossChanceMin, 
            float packetLossChanceMax)
        {
            m_Name = name;
            m_Transporter = transporter;
            m_PacketPool = new ObjectPool<Packet>(
                createFunc: () =>
                {
                    return new Packet();
                });

            UnityEngine.Debug.Assert(
                RTTMax >= RTTMin &&
                RTTMax >= 0 &&
                RTTMin >= 0);
            UnityEngine.Debug.Assert(
                packetLossChanceMax >= packetLossChanceMin &&
                packetLossChanceMax >= 0 && packetLossChanceMax <= 1 &&
                packetLossChanceMin >= 0 && packetLossChanceMin <= 1);

            m_RTTMin = RTTMin;
            m_RTTMax = RTTMax;
            m_PacketLossChanceMin = packetLossChanceMin;
            m_PacketLossChanceMax = packetLossChanceMax;

            m_Random = IRandom.Create();
        }

        public void Update()
        {
            while (m_Queue.Count > 0)
            {
                var packet = m_Queue.Peek();
                if (packet.Ready)
                {
                    m_Queue.Dequeue();
                    m_Transporter.Send(packet.Stream);
                    ReleasePacket(packet);

                    if (m_Queue.Count > 0)
                    {
                        var nextPacket = m_Queue.Peek();
                        nextPacket.StartTimer();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void Send(ByteStream stream)
        {
            m_Queue.Enqueue(GetPacket(GetLatency(), stream));
        }

        private int GetLatency()
        {
            var latency = GetRandomLatency();
            while (true)
            {
                if (m_Random.Value <= m_Random.NextFloat(m_PacketLossChanceMin, m_PacketLossChanceMax))
                {
                    //packet lost, add latency
                    latency += GetRandomLatency();
                }
                else
                {
                    break;
                }
            }
            return latency;
        }

        private int GetRandomLatency()
        {
            return m_Random.NextInt(m_RTTMin, m_RTTMax + 1);
        }

        private void ReleasePacket(Packet message)
        {
            m_PacketPool.Release(message);
        }

        private Packet GetPacket(int latency, ByteStream stream)
        {
            var packet = m_PacketPool.Get();
            packet.Init(++m_NextPacketIndex, latency, stream);
            return packet;
        }

        private int m_NextPacketIndex;
        private readonly string m_Name;
        private readonly int m_RTTMin;
        private readonly int m_RTTMax;
        private readonly float m_PacketLossChanceMin;
        private readonly float m_PacketLossChanceMax;
        private readonly IObjectPool<Packet> m_PacketPool;
        private readonly ITransportable m_Transporter;
        private readonly IRandom m_Random;
        private readonly Queue<Packet> m_Queue = new();

        private class Packet
        {
            public int Index => m_Index;
            public bool Ready => Timer.ElapsedMilliseconds >= m_Latency;
            public ByteStream Stream => m_Stream;
            public int Latency => m_Latency;
            public Stopwatch Timer => m_Timer;

            public void Init(int index, int latency, ByteStream stream)
            {
                m_Index = index;
                m_Latency = latency;
                m_Stream = stream;
                StartTimer();
            }

            public void StartTimer()
            {
                Timer.Restart();
            }

            private readonly Stopwatch m_Timer = new();
            private ByteStream m_Stream;
            private int m_Index;
            private int m_Latency;
        }
    }

    public class QualityStageSimpleCreator : IQualityStageCreator
    {
        public QualityStageSimpleCreator(
            int RTTMin,
            int RTTMax,
            float packetLossChanceMin, 
            float packetLossChanceMax)
        {
            m_RTTMin = RTTMin;
            m_RTTMax = RTTMax;
            m_PacketLossChanceMin = packetLossChanceMin;
            m_PacketLossChanceMax = packetLossChanceMax;
        }

        public IQualityStage Create(string name, ITransportable transportable)
        {
            return new QualityStageSimple(name, transportable, m_RTTMin, m_RTTMax, m_PacketLossChanceMin, m_PacketLossChanceMax);
        }

        private readonly int m_RTTMin;
        private readonly int m_RTTMax;
        private readonly float m_PacketLossChanceMin;
        private readonly float m_PacketLossChanceMax;
    }
}

//XDay