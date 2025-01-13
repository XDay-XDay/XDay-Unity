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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NetworkAPI
{
    internal class AsyncStreamTransporterTCP : ITransporter
    {
        public Socket Socket => m_Socket;
        public bool IsConnected => m_Socket.Connected;

        public AsyncStreamTransporterTCP(Socket socket, INetworkContext context, OnReceiveData onReceiveData, Action onSendComplete)
        {
            m_Socket = socket;
            m_OnReceiveData = onReceiveData;
            m_OnSendFinish = onSendComplete;
            m_Context = context;

            m_ArgsPool = new ConcurrentObjectPool<SocketAsyncEventArgs>(
            createFunc: () =>
            {
                return new SocketAsyncEventArgs();
            },
            capacity: 10,
            actionOnGet:null,
            actionOnDestroy: ResetArgs,
            actionOnRelease: ResetArgs);

            m_ReceiveArgs = new SocketAsyncEventArgs();
            m_ReceiveArgs.Completed -= OnIOCompleted;
            m_ReceiveArgs.Completed += OnIOCompleted;
            var buffer = m_Context.ArrayPool.Get(4096);
            m_ReceiveArgs.SetBuffer(buffer, 0, buffer.Length);
        }

        public void OnDestroy(bool closeSocket)
        {
            if (closeSocket)
            {
                CloseSocket(SocketShutdown.Both);
            }

            m_Context.ArrayPool.Release(m_ReceiveArgs.Buffer);
            m_ReceiveArgs.SetBuffer(null);
            m_ArgsPool.Clear();
        }

        public void CloseSocket(SocketShutdown shutdown)
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
                Debug.Log($"connection closed!");
            }
        }

        public void Receive()
        {
            if (!m_Socket.ReceiveAsync(m_ReceiveArgs))
            {
                OnReceiveFinish();
            }
        }

        public void Send(ByteStream stream)
        {
            if (m_Socket.Connected)
            {
                var args = GetSendArgs(stream);

                if (!m_Socket.SendAsync(args))
                {
                    OnSendFinish(args);
                }
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

        private void OnSendFinish(SocketAsyncEventArgs args)
        {
            m_Context.ReleaseByteStream(args.UserToken as ByteStream);

            ReleaseSendArgs(args);

            if (args.SocketError == SocketError.Success)
            {
                m_OnSendFinish?.Invoke();
            }
            else
            {
                Debug.LogError("Send failed!");
                CloseSocket(SocketShutdown.Send);
            }
        }

        /// <summary>
        /// called in worker thread
        /// </summary>
        private void OnReceiveFinish()
        {
            if (m_ReceiveArgs.SocketError == SocketError.Success && 
                m_ReceiveArgs.BytesTransferred > 0)
            {
                try
                {
                    m_OnReceiveData(m_ReceiveArgs.Buffer, m_ReceiveArgs.Offset, m_ReceiveArgs.BytesTransferred);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                Receive();
            }
            else
            {
                CloseSocket(SocketShutdown.Both);
            }
        }

        private void OnIOCompleted(object sender, SocketAsyncEventArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    OnReceiveFinish();
                    break;
                case SocketAsyncOperation.Send:
                    OnSendFinish(args);
                    break;
                default:
                    throw new ArgumentException("Invalid operation");
            }
        }

        private void ReleaseSendArgs(SocketAsyncEventArgs args)
        {
            args.SetBuffer(null, 0, 0);
            m_ArgsPool.Release(args);
        }

        private SocketAsyncEventArgs GetSendArgs(ByteStream stream)
        {
            var args = m_ArgsPool.Get();
            if (args.UserToken == null)
            {
                args.Completed += OnIOCompleted;
            }

            args.UserToken = stream;
            args.SetBuffer(stream.Buffer, 0, (int)stream.Length);

            return args;
        }

        private void ResetArgs(SocketAsyncEventArgs args)
        {
            args.SetBuffer(null, 0, 0);
            args.Completed -= OnIOCompleted;
            args.UserToken = null;
            if (args.AcceptSocket != null)
            {
                args.AcceptSocket.Close();
                args.AcceptSocket = null;
            }
        }

        private readonly Socket m_Socket;
        private readonly ConcurrentObjectPool<SocketAsyncEventArgs> m_ArgsPool;
        private readonly Action m_OnSendFinish;
        private readonly SocketAsyncEventArgs m_ReceiveArgs;
        private readonly OnReceiveData m_OnReceiveData;
        private readonly INetworkContext m_Context;
        internal delegate void OnReceiveData(byte[] buffer, int offset, int size);
    }
}

//XDay