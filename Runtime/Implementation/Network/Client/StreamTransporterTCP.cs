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
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal class StreamTransporterTCP : ITransporter
    {
        public bool Connected => m_Socket.Connected;
        public Socket Socket => m_Socket;

        public StreamTransporterTCP(
            Socket socket, 
            OnReceiveData onReceiveData, 
            INetworkContext context)
        {
            m_Socket = socket;
            m_ReceiveBuffer = context.ArrayPool.Get(4096);
            m_OnReceiveData = onReceiveData;
            m_Context = context;
        }

        public void OnDestroy(bool closeSocket)
        {
            m_Context.ArrayPool.Release(m_ReceiveBuffer);
            if (closeSocket)
            {
                Close(SocketShutdown.Both);
            }
        }

        public void Send(ByteStream stream)
        {
            if (m_Socket.Connected)
            {
                var leftSize = (int)stream.Length;
                var offset = 0;
                try
                {
                    while (true)
                    {
                        var n = m_Socket.Send(stream.Buffer, offset, leftSize, SocketFlags.None);
                        leftSize -= n;
                        offset += n;
                        if (leftSize <= 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"StreamTransporterTCP send failed {e}");
                    Close(SocketShutdown.Send);
                }
                finally
                {
                    m_Context.ReleaseByteStream(stream);
                }
            }
        }

        public void Receive()
        {
            try
            {
                if (m_Socket.Connected)
                {
                    var bytesReceived = m_Socket.Receive(m_ReceiveBuffer);
                    if (bytesReceived > 0)
                    {
                        m_OnReceiveData(m_ReceiveBuffer, 0, bytesReceived);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is not ThreadAbortException && 
                    (e is not SocketException se || se.SocketErrorCode != SocketError.Interrupted))
                {
                    Debug.LogError($"StreamTransporterTCP receive failed: {e}");
                }
                Close(SocketShutdown.Both);
            }
        }

        public void Close(SocketShutdown shutdown)
        {
            try
            {
                m_Socket.Shutdown(shutdown);
            }
            catch (Exception)
            {
            }
            finally
            {
                m_Socket.Close();
            }
        }

        public TransporterState Update()
        {
            if (m_Socket.Connected)
            {
                return TransporterState.Running;
            }
            return TransporterState.Done;
        }

        private readonly Socket m_Socket;
        private readonly byte[] m_ReceiveBuffer;
        private readonly OnReceiveData m_OnReceiveData;
        private readonly INetworkContext m_Context;
        internal delegate void OnReceiveData(byte[] buffer, int offset, int size);
    }
}

//XDay