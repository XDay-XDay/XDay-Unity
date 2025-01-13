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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal class ServerTCP : IServer
    {
        public int ConnectionCount => m_Connections.Count;
        public bool EnableTCPDelay
        {
            get => m_EnableTCPDelay;
            set
            {
                m_EnableTCPDelay = value;
                for (int i = 0; i < m_Connections.Count; ++i)
                {
                    m_Connections[i].EnableTCPDelay = value;
                }
            }
        }

        public ServerTCP(INetworkContext context, Action<IConnection, object> onProtocalReceived)
        {
            m_Context = context;
            m_OnProtocalReceived = onProtocalReceived;
        }

        public void OnDestroy()
        {
            Stop();
            for (int i = m_Connections.Count - 1; i >= 0; --i)
            {
                RemoveConnection(i);
            }
        }

        public void Start(int port)
        {
            if (m_Running)
            {
                Debug.LogError("Server already started!");
                return;
            }

            m_Running = true;

            IPEndPoint endPoint = new(IPAddress.Any, port);

            m_ListenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_ListenSocket.Bind(endPoint);
            m_ListenSocket.Listen(10);

            SocketAsyncEventArgs acceptEventArg = new();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            Accept(acceptEventArg);
        }

        public void Stop()
        {
            m_Running = false;

            m_ListenSocket.Close();
            m_ListenSocket = null;

            lock (m_Lock)
            {
                foreach (var connection in m_Connections)
                {
                    connection.OnDestroy();
                }
                m_Connections.Clear();
            }
        }

        public void Update()
        {
            lock (m_Lock)
            {
                for (int i = 0; i < m_Connections.Count; ++i)
                {
                    if (m_Connections[i].Update() == TransporterState.Done)
                    {
                        RemoveConnection(i);
                    }
                }
            }
        }

        private void Accept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;
            if (!m_ListenSocket.AcceptAsync(args))
            {
                OnAcceptCompleted(null, args);
            }
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs acceptArgs)
        {
            Socket clientSocket = acceptArgs.AcceptSocket;

            ServerConnectionTCP connection = new(
                ++m_NextConnectionID, 
                clientSocket, 
                m_EnableTCPDelay, 
                null, 
                m_OnProtocalReceived, 
                m_Context.Config.CreateHandShaker(clientSocket, m_Context),
                m_Context);
            AddConnection(connection);

            Accept(acceptArgs);
        }

        private void AddConnection(ServerConnectionTCP connection)
        {
            lock (m_Lock)
            {
                m_Connections.Add(connection);
            }
        }

        private void RemoveConnection(int index)
        {
            if (index >= 0 && index < m_Connections.Count)
            {
                m_Connections[index].OnDestroy();
                m_Connections.RemoveAt(index);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private readonly object m_Lock = new();
        private readonly Action<IConnection, object> m_OnProtocalReceived;
        private readonly List<ServerConnectionTCP> m_Connections = new();
        private Socket m_ListenSocket;
        private readonly INetworkContext m_Context;
        private int m_NextConnectionID;
        private bool m_Running = false;
        private bool m_EnableTCPDelay = true;
    }
}

//XDay