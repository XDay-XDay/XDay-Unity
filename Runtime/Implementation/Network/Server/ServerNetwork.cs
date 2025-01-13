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

namespace XDay.NetworkAPI
{
    internal partial class ServerNetwork : IServerNetwork
    {
        public ServerNetwork(INetworkConfig config)
        {
            m_Context = new NetworkContext("Server", config);
            m_Server = m_Context.CreateServer(true, (connection, protocal) => {
                m_MessageQueue.Enqueue(new ProtocalSource(connection, protocal));
            });
        }

        public void OnDestroy()
        {
            m_Server.OnDestroy();
            m_Context.OnDestroy();
        }

        public void StartServer(int port)
        {
            m_Server.Start(port);
        }

        public void StopServer()
        {
            m_Server.Stop();
        }

        public void RegisterProtocal<T>() where T : class, new()
        {
            m_Context.RegisterProtocal<T>();
        }

        public void RegisterProtocalHandler<T>(Action<IConnection, T> handler)
        {
            m_ProtocalHandlers[typeof(T)] = (IConnection connection, object msg) => {
                handler(connection, (T)msg);
            };
        }

        public void Update(float dt)
        {
            m_TempList.Clear();
            while (m_MessageQueue.TryDequeue(out var source))
            {
                m_TempList.Add(source);
            }

            for (int i = 0; i < m_TempList.Count; ++i)
            {
                var msg = m_TempList[i];
                var type = msg.Protocal.GetType();
                m_ProtocalHandlers[type](msg.Connection, msg.Protocal);
            }

            m_Server.Update();
        }

        public void Broadcast(object notify, IConnection exception)
        {
            foreach (var kv in m_Users)
            {
                if (kv.Value.Connection != exception)
                {
                    kv.Value.Connection.Send(notify);
                }
            }
        }

        public void AddUser(ServerUser user)
        {
            m_Users.Add(user.UserID, user);
        }

        public ServerUser GetUser(int userID)
        {
            m_Users.TryGetValue(userID, out var user);
            return user;
        }

        private readonly IServer m_Server;
        private readonly NetworkContext m_Context;
        private readonly Dictionary<long, ServerUser> m_Users = new();        
        private readonly ConcurrentQueue<ProtocalSource> m_MessageQueue = new();
        private readonly List<ProtocalSource> m_TempList = new();
        private readonly Dictionary<Type, Action<IConnection, object>> m_ProtocalHandlers = new();

        private readonly struct ProtocalSource
        {
            public IConnection Connection { get; }
            public object Protocal { get; }

            public ProtocalSource(IConnection connection, object protocal)
            {
                Connection = connection;
                Protocal = protocal;
            }
        }
    }

    public class User
    {
        public long UserID { get { return m_UserID; } }
        public string Name { get { return m_Name; } }

        public User(long userID, string name)
        {
            m_Name = name;
            m_UserID = userID;
        }

        private readonly long m_UserID;
        private readonly string m_Name;
    }

    public class ServerUser : User
    {
        public IConnection Connection => m_Connection;

        public ServerUser(int userID, string name, IConnection connection)
            : base(userID, name)
        {
            m_Connection = connection;
        }

        private readonly IConnection m_Connection;
    }
}

//XDay