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
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal partial class ClientConnectionTCP
    {
        private class StateTransport : State
        {
            public StreamTransporterTCP Transporter => m_Transporter;

            public StateTransport(
                ClientConnectionTCP connection, 
                Socket socket, 
                Action<IConnection, object> onProtocalReceived, 
                INetworkContext context)
            {
                m_Transporter = new StreamTransporterTCP(socket, OnReceiveData, context);
                m_ProtocalReceivedCallback = onProtocalReceived;
                m_Connection = connection;
            }

            public override void OnDestroy()
            {
                m_Transporter?.OnDestroy(true);
                m_SendThreadQuit = true;
                m_ReceiveThreadQuit = true;
            }

            public override void OnEnter()
            {
                m_ReceiveThread = new Thread(ReceiveThreadLoop);
                m_SendThread = new Thread(SendThreadLoop);
                m_ReceiveThread.Start();
                m_SendThread.Start();
            }

            public override TransporterState Update()
            {
                return m_Transporter.Update();
            }

            public void Send(object protocal)
            {
                m_SendQueue.Enqueue(protocal);
            }

            public void Close(SocketShutdown shutdown)
            {
                m_Transporter.Close(shutdown);
            }

            private void ReceiveThreadLoop()
            {
                while (!m_ReceiveThreadQuit)
                {
                    m_Transporter.Receive();
                }
            }

            private void OnReceiveData(byte[] buffer, int offset, int size)
            {
                m_Connection.m_MessagePipeline.Receive(buffer, offset, size, (protocal) => {
                    m_ProtocalReceivedCallback(m_Connection, protocal);
                }
                );
            }

            private void SendThreadLoop()
            {
                while (!m_SendThreadQuit)
                {
                    m_Connection.m_MessagePipeline.Update();

                    if (m_SendQueue.TryDequeue(out var protocal))
                    {
                        if (m_Transporter.Connected)
                        {
                            m_Connection.m_MessagePipeline.Send(protocal);
                        }
                        else
                        {
                            Debug.LogError("send failed");
                        }
                    }
                }
            }

            private readonly StreamTransporterTCP m_Transporter;
            private readonly ConcurrentQueue<object> m_SendQueue = new();
            private volatile bool m_SendThreadQuit = false;
            private volatile bool m_ReceiveThreadQuit = false;
            private Thread m_SendThread;
            private Thread m_ReceiveThread;
            private readonly ClientConnectionTCP m_Connection;
            private readonly Action<IConnection, object> m_ProtocalReceivedCallback;
        }
    }
}

//XDay