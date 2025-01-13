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
    public interface IClientNetwork
    {
        static IClientNetwork Create(INetworkConfig config)
        {
            return new ClientNetwork(config);
        }

        void OnDestroy();
        void RegisterProtocal<T>() where T : class, new();
        void RegisterProtocal(Type type);
        void RegisterProtocalHandler<T>(Action<IConnection, T> handler);
        void RegisterProtocalHandler(Type type, Action<IConnection, object> handler);
        void UnregisterProtocalHandler(Type type);
        void RegisterAckHandler(Action<object> handler);
        void Connect(string ip, int port, Action<bool> onConnected);
        void Disconnect(bool cleanData);
        void CancelConnection();
        void Send(object protocal);
        void Update(float dt);
    }

    public interface IMessagePipeline
    {
        static IMessagePipeline Create(INetworkContext controller,
            IProtocalStage protocalStage,
            ICompressionStage compressionStage,
            IEncryptionStage encryptionStage,
            IFlowControlStage flowControlStage,
            IQualityStage qualityStage)
        {
            return new MessagePipeline(controller, protocalStage, compressionStage, encryptionStage, flowControlStage, qualityStage);
        }

        void OnDestroy();
        void Send(object protocal);
        void Receive(byte[] buffer, int offset, int size, Action<object> onProtocalDecoded);
        void Update();
    }
}

//XDay