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

namespace XDay.NetworkAPI
{
    internal class NetworkContext : INetworkContext
    {
        public string Name => m_Name;
        public INetworkConfig Config => m_Config;
        public ITransportStage TransportStage => m_TransportStage;
        public IProtocalStage ProtocalStage => m_ProtocalStage;
        public IArrayPool ArrayPool => m_ArrayPool;

        public NetworkContext(string name, INetworkConfig config)
        {
            m_Name = name;
            m_ArrayPool = new ArrayPool(1024);
            m_StreamPool = new ByteStreamPool(m_ArrayPool);
            m_Config = config;
            m_ProtocalStage = config.CreateProtocalStage();
            m_TransportStage = config.CreateTransportStage(this);
        }

        public void OnDestroy()
        {
            m_ProtocalStage.OnDestroy();
            m_TransportStage.OnDestroy();
        }

        public IClient CreateClient(bool enableTCPDelay, Action<IConnection, object> onProtocalReceived)
        {
            return m_TransportStage.CreateClient(enableTCPDelay, onProtocalReceived);
        }

        public IServer CreateServer(bool enableTCPDelay, Action<IConnection, object> onProtocalReceived)
        {
            return m_TransportStage.CreateServer(enableTCPDelay, onProtocalReceived);
        }

        public void RegisterProtocal(Type type)
        {
            m_ProtocalStage.RegisterProtocal(type);
        }

        public void RegisterProtocal<T>() where T : class, new()
        {
            m_ProtocalStage.RegisterProtocal<T>();
        }

        public ByteStream GetByteStream()
        {
            return m_StreamPool.Get();
        }

        public void ReleaseByteStream(ByteStream stream)
        {
            m_StreamPool.Release(stream);
        }

        private readonly string m_Name;
        private readonly IProtocalStage m_ProtocalStage;
        private readonly ITransportStage m_TransportStage;
        private readonly INetworkConfig m_Config;
        private readonly ArrayPool m_ArrayPool;
        private readonly ByteStreamPool m_StreamPool;
    }
}

//XDay