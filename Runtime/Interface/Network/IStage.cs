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
    public interface IProtocalStage
    {
        void OnDestroy() { }
        void Encode(object protocal, ByteStream output);
        object Decode(ByteStream input);
        void RegisterProtocal(Type type);
        void RegisterProtocal<T>() where T : class, new();
    }

    public interface IProtocalStageCreator
    {
        IProtocalStage Create();
    }

    public interface ICompressionStage
    {
        void OnDestroy() { }
        void Compress(ByteStream input, ByteStream output);
        void Decompress(ByteStream input, ByteStream output);
    }

    public interface ICompressionStageCreator
    {
        ICompressionStage Create();
    }

    public interface IEncryptionStage
    {
        void OnDestroy() { }
        int Encrypt(ByteStream input, ByteStream output);
        int Decrypt(ByteStream input, ByteStream output);
    }

    public interface IEncryptionStageCreator
    {
        IEncryptionStage Create(object data);
    }

    public interface IQualityStage : ITransportable
    {
        void OnDestroy() { }
        void Update();
    }

    public interface IQualityStageCreator
    {
        IQualityStage Create(string name, ITransportable transportable);
    }

    public interface IFlowControlStage
    {
        void OnDestroy() { }
        bool CheckOverflow(ByteStream stream);
        void Update();
    }

    public interface IFlowControlStageCreator
    {
        IFlowControlStage Create(ITransportable transportable, INetworkContext controller);
    }

    public interface ITransportStage
    {
        void OnDestroy() { }
        IClient CreateClient(bool enableTCPDelay, Action<IConnection, object> onProtocalReceived);
        IServer CreateServer(bool enableTCPDelay, Action<IConnection, object> onProtocalReceived);
    }

    public interface ITransportStageCreator
    {
        ITransportStage Create(INetworkContext context);
    }

    public interface IClient
    {
        int ConnectionCount { get; }

        void OnDestroy();
        bool Connect(string ip, int port, Action<IConnection> onConnectionEstablished, out ISocketCancellationToken handle);
        void Update();
        void CancelConnections();
    }

    public interface IServer
    {
        int ConnectionCount { get; }

        void OnDestroy();
        void Start(int port);
        void Stop();
        void Update();
    }
}

//XDay