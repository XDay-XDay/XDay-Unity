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


namespace XDay.NetworkAPI
{
    public enum TransporterState
    {
        Running,
        Done,
    }

    public interface ITransportable
    {
        //stream will be released by send operation
        void Send(ByteStream stream);
    }

    public interface ITransporter : ITransportable
    {
        void OnDestroy(bool closeSocket);
        TransporterState Update();
    }

    public interface IConnection
    {
        int ID { get; }
        string RemoteIP { get; }
        int RemotePort { get; }
        ITransporter Transporter { get; }

        void OnDestroy();
        void Disconnect();
        void Send(object protocal);
        TransporterState Update();
    }

    public interface IConnectionTCP : IConnection
    {
        bool EnableTCPDelay { set; }
    }

    public interface ISocketCancellationToken
    {
        string IP { get; }
        int Port { get; }

        void Cancel();
    }

    public interface IDataBlock
    {
        static IDataBlock Create(int size, IArrayPool pool)
        {
            return new DataBlock(size, pool);
        }

        byte[] Buffer { get; }
        int Size { get; }

        void OnDestroy();
        void SetSize(int bufferSize);
    }

    public interface IArrayPool
    {
        void Release(byte[] array);
        byte[] Get(int length);
    }
}

//XDay