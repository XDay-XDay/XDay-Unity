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
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal class ClientTCP : IClient, ISocketCancellationCancelTokenManager
    {
        public int ConnectionCount => m_Connections.Count;
        public bool EnableTCPDelay
        {
            get => m_EnableTCPDelay;
            set
            {
                m_EnableTCPDelay = value;
                for (var i = 0; i < m_Connections.Count; ++i)
                {
                    m_Connections[i].EnableTCPDelay = value;
                }
            }
        }

        public ClientTCP(INetworkContext context, Action<IConnection, object> onProtocalReceived)
        {
            m_Context = context;
            m_OnProtocalReceived = onProtocalReceived;
        }

        public void OnDestroy()
        {
            lock (m_Connections)
            {
                for (var i = m_Connections.Count - 1; i >= 0; --i)
                {
                    RemoveConnectionInternal(i);
                }
            }

            CancelConnections();
        }

        public bool Connect(
            string ip, 
            int port, 
            Action<IConnection> onConnectionEstablished,
            out ISocketCancellationToken token)
        {
            if (!ValidateAddress(ip, port))
            {
                token = null;
                return false;
            }

            var address = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            token = new SocketCancellationToken(socket, address, this);
            m_Tokens.Add(token);

            socket.BeginConnect(address, (result) => {
                try
                {
                    socket.EndConnect(result);
                    var connection = new ClientConnectionTCP(
                        ++m_NextConnectionID, 
                        m_Context.Config.CreateHandShaker(socket, m_Context), 
                        socket, 
                        m_OnProtocalReceived, 
                        onConnectionEstablished, 
                        m_EnableTCPDelay, 
                        m_Context);
                    m_Connections.Add(connection);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Connect failed: {e}");
                    onConnectionEstablished(null);
                }
                finally
                {
                    m_Tokens.Remove(result.AsyncState as ISocketCancellationToken);
                }
            }, token);

            return true;
        }

        public void CancelConnections()
        {
            foreach (var token in m_Tokens)
            {
                token.Cancel();
            }
            m_Tokens.Clear();
        }

        public void RemoveToken(ISocketCancellationToken handle)
        {
            m_Tokens.Remove(handle);
        }

        public void Update()
        {
            lock (m_Lock)
            {
                for (var i = m_Connections.Count - 1; i >= 0; --i)
                {
                    if (m_Connections[i].Update() == TransporterState.Done)
                    {
                        RemoveConnectionInternal(i);
                    }
                }
            }
        }

        private bool ValidateAddress(string ip, int port)
        {
            foreach (var connection in m_Connections)
            {
                if (connection.RemoteIP == ip && connection.RemotePort == port)
                {
                    Debug.LogError($"Already connected to {ip}:{port}");
                    return false;
                }
            }

            foreach (var token in m_Tokens)
            {
                if (token.IP == ip && token.Port == port)
                {
                    Debug.LogError($"Is connecting to {ip}:{port}");
                    return false;
                }
            }

            return true;
        }

        private void RemoveConnectionInternal(int index)
        {
            m_Connections[index].OnDestroy();
            m_Connections.RemoveAt(index);
        }

        private bool m_EnableTCPDelay;
        private int m_NextConnectionID;
        private readonly INetworkContext m_Context;
        private readonly object m_Lock = new();
        private readonly Action<IConnection, object> m_OnProtocalReceived;
        private readonly List<IConnectionTCP> m_Connections = new();
        private readonly List<ISocketCancellationToken> m_Tokens = new();
    }
}

//XDay