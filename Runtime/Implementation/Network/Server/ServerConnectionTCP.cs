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
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace XDay.NetworkAPI
{
    /// <summary>
    /// using thread pool, gc when using queue operation
    /// </summary>
    partial class ServerConnectionTCP : IConnectionTCP
    {
        public int ID => m_ID;
        public ITransporter Transporter => m_Transporter;
        public Socket Socket => m_Transporter.Socket;
        public string RemoteIP => m_RemoteIP;
        public int RemotePort => m_RemotePort;
        public bool EnableTCPDelay
        {
            set => m_Transporter.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, !value);
        }

        public ServerConnectionTCP(
            int id, 
            Socket socket, 
            bool enableTCPDelay,
            Action<IConnection> onConnectFinish,
            Action<IConnection, object> onProtocalDecoded,
            IHandShaker handShaker,
            INetworkContext context)
        {
            Debug.Assert(handShaker != null);

            var ip = socket.RemoteEndPoint as IPEndPoint;
            m_RemoteIP = ip.Address.ToString();
            m_RemotePort = ip.Port;
            m_IsConnecting = true;
            m_ID = id;
            m_ProtocalDecodedCallback = onProtocalDecoded;
            m_Transporter = new AsyncStreamTransporterTCP(socket, context, OnReceiveData, OnSendComplete);

            handShaker.Start(
                connection: this,
                (IMessagePipeline pipeline) =>
                {
                    OnConnectionEstablished(onConnectFinish, pipeline);
                }
            );
            m_HandShaker = handShaker;
            EnableTCPDelay = enableTCPDelay;
        }

        public void OnDestroy()
        {
            m_Transporter.OnDestroy(true);
            m_MessagePipeline?.OnDestroy();
        }

        public void CloseSocket(SocketShutdown shutdown)
        {
            m_Transporter.CloseSocket(shutdown);
        }

        public void Disconnect()
        {
            CloseSocket(SocketShutdown.Both);
        }

        public void Send(object protocal)
        {
            m_SendQueue.Enqueue(protocal);
            if (m_SendQueue.Count == 1)
            {
                Flush();
            }
        }

        public TransporterState Update()
        {
            if (m_HandShaker != null)
            {
                var state = m_HandShaker.Update();
                if (state == TransporterState.Done)
                {
                    m_HandShaker.OnDestroy(closeSocket: false);
                    m_HandShaker = null;

                    if (m_IsConnecting)
                    {
                        return TransporterState.Done;
                    }
                }
            }
            else
            {
                m_MessagePipeline.Update();
                return m_Transporter.Update();
            }

            return TransporterState.Running;
        }

        private void OnConnectionEstablished(Action<IConnection> onConnectFinish, IMessagePipeline pipeline)
        {
            m_IsConnecting = false;
            m_MessagePipeline = pipeline;
            onConnectFinish?.Invoke(this);
            m_Transporter.Receive();
        }

        private void OnReceiveData(byte[] buffer, int offset, int size)
        {
            m_MessagePipeline.Receive(buffer, offset, size, OnProtocalDecoded);
        }

        private void OnProtocalDecoded(object protocal)
        {
            m_ProtocalDecodedCallback(this, protocal);
        }

        private void Flush()
        {
            if (m_SendQueue.TryDequeue(out var protocal))
            {
                if (!m_Transporter.IsConnected)
                {
                    Debug.LogError("send failed, transporter is not connected");
                }
                else
                {
                    m_MessagePipeline.Send(protocal);
                }
            }
        }

        private void OnSendComplete()
        {
            Flush();
        }

        private readonly int m_ID;
        private readonly string m_RemoteIP;
        private readonly int m_RemotePort;
        private readonly ConcurrentQueue<object> m_SendQueue = new();
        private readonly Action<IConnection, object> m_ProtocalDecodedCallback;
        private readonly AsyncStreamTransporterTCP m_Transporter;
        private IMessagePipeline m_MessagePipeline;
        private IHandShaker m_HandShaker;
        private bool m_IsConnecting;
    }
}

//XDay