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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal class ClientNetwork : IClientNetwork
    {
        public ClientNetwork(INetworkConfig config)
        {
            m_Context = new NetworkContext("Client", config);
            m_Client = m_Context.CreateClient(true, (connection, protocal) => {
                m_ReceiveQueue.Enqueue(new ProtocalSource(connection, protocal));
            });
        }

        public void OnDestroy()
        {
            m_Client?.OnDestroy();
            m_Context?.OnDestroy();
        }

        public void RegisterProtocal<T>() where T : class, new()
        {
            m_Context.RegisterProtocal<T>();
        }

        public void RegisterProtocal(Type type)
        {
            m_Context.RegisterProtocal(type);
        }

        public void RegisterProtocalHandler<T>(Action<IConnection, T> handler)
        {
            if (!m_Dispatchers.ContainsKey(typeof(T)))
            {
                m_Dispatchers[typeof(T)] = (connection, msg) =>
                {
                    handler?.Invoke(connection, (T)msg);
                };
            }
            else
            {
                Debug.LogError($"Handler of type {typeof(T)} already registered!");
            }
        }

        public void RegisterProtocalHandler(Type type, Action<IConnection, object> handler)
        {
            if (!m_Dispatchers.ContainsKey(type))
            {
                m_Dispatchers[type] = (connection, msg) =>
                {
                    handler?.Invoke(connection, msg);
                };
            }
            else
            {
                Debug.LogError($"Handler of type {type} already registered!");
            }
        }

        public void UnregisterProtocalHandler(Type type)
        {
            m_Dispatchers.Remove(type);
        }

        public void Connect(string ip, int port, Action<bool> onConnected)
        {
            m_Client.Connect(ip, port, (connection) =>
                {
                    m_Connection = connection;
                    onConnected.Invoke(connection != null);
                }, out m_ConnectionCancellationToken);
        }

        public void Disconnect(bool clearAll)
        {
            m_Connection?.Disconnect();
            m_Connection = null;
            m_Client?.OnDestroy();
            if (clearAll)
            {
                m_Dispatchers.Clear();
            }
        }

        public void CancelConnection()
        {
            m_ConnectionCancellationToken?.Cancel();
            m_ConnectionCancellationToken = null;
        }

        public void RegisterAckHandler(Action<object> handler)
        {
            m_AckHandler = handler;
        }

        public void Send(object protocal)
        {
            if (m_Connection != null)
            {
                m_Connection.Send(protocal);
            }
            else
            {
                Debug.LogError($"not connected!");
            }
        }

        public void Update(float dt)
        {
            try
            {
                Dispatch();

                m_Client?.Update();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void Dispatch()
        {
            m_TempList.Clear();
            while (m_ReceiveQueue.TryDequeue(out var protocal))
            {
                m_TempList.Add(protocal);
                if (m_ProtocalFilter != null &&
                    !m_ProtocalFilter(protocal.ProtocalObject))
                {
                    break;
                }
            }

            foreach (var source in m_TempList)
            {
                if (source.ProtocalObject != null)
                {
                    var type = source.ProtocalObject.GetType();
                    if (m_Dispatchers.ContainsKey(type))
                    {
                        m_Dispatchers[type]?.Invoke(source.Connection, source.ProtocalObject);
                    }
                    if (type.Name.EndsWith("Ack"))
                    {
                        m_AckHandler?.Invoke(source.ProtocalObject);
                    }
                }
            }
        }

        private readonly IClient m_Client;
        private IConnection m_Connection;
        private readonly ConcurrentQueue<ProtocalSource> m_ReceiveQueue = new();
        private ISocketCancellationToken m_ConnectionCancellationToken;
        private readonly Dictionary<Type, Action<IConnection, object>> m_Dispatchers = new();
        private readonly List<ProtocalSource> m_TempList = new();
        private Action<object> m_AckHandler;
        private readonly Func<object, bool> m_ProtocalFilter;
        private readonly NetworkContext m_Context;

        private readonly struct ProtocalSource
        {
            public ProtocalSource(IConnection connection, object protocal)
            {
                Connection = connection;
                ProtocalObject = protocal;
            }

            public IConnection Connection { get; }
            public object ProtocalObject { get; }
        }
    }
}


//XDay