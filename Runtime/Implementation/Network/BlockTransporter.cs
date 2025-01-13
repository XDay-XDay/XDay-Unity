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
using UnityEngine.Pool;

namespace XDay.NetworkAPI
{
    public class BlockTransporter : ITransporter
    {
        public Action<IDataBlock> DataReceivedCallback { set => m_DataReceivedCallback = value; }

        public BlockTransporter(Socket socket, INetworkContext context)
        {
            m_Context = context;
            m_Socket = socket;
            m_Socket.Blocking = false;
            m_ArgPool = new ObjectPool<SocketAsyncEventArgs>(
                createFunc: () =>
                {
                    return new SocketAsyncEventArgs();
                },
                actionOnDestroy: ResetArgs,
                actionOnRelease: ResetArgs);
        }

        public void OnDestroy(bool closeSocket)
        {
            m_ArgPool.Clear();
            if (closeSocket)
            {
                CloseSocket(SocketShutdown.Both);
            }
        }

        public void Send(ByteStream stream)
        {
            if (m_Socket.Connected)
            {
                var args = GetSendArgs(stream);
                if (!m_Socket.SendAsync(args))
                {
                    OnSendComplete(args);
                }
            }
            else
            {
                Debug.LogError("Send failed, socket is not connected!");
            }
        }

        public IDataBlock Receive(int size)
        {
            m_Block = new DataBlock(size, m_Context.ArrayPool);
            return m_Block;
        }

        public TransporterState Update()
        {
            if (m_Block == null ||
                m_Block.Error != SocketError.Success)
            {
                return TransporterState.Done;
            }

            try
            {
                if (!m_Block.Full)
                {
                    if (m_Socket.Poll(0, SelectMode.SelectRead))
                    {
                        while (true)
                        {
                            var n = m_Socket.Receive(
                                m_Block.Buffer, 
                                m_Block.ReadCount, 
                                m_Block.LeftSize, 
                                SocketFlags.None,
                                out var error);
                            m_Block.Error = error;

                            if (n == 0 ||
                                error != SocketError.Success ||
                                m_Block.Skip(n))
                            {
                                break;
                            }
                        }

                        if (m_Block.Error == SocketError.WouldBlock)
                        {
                            m_Block.Error = SocketError.Success;
                        }
                        if (m_Block.Full)
                        {
                            m_DataReceivedCallback.Invoke(m_Block);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.LogError($"Exception: {e}");
                return TransporterState.Done;
            }
            return TransporterState.Running;
        }

        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    OnSendComplete(e);
                    break;
                default:
                    throw new ArgumentException("Invalid operation");
            }
        }

        private void OnSendComplete(SocketAsyncEventArgs sendArgs)
        {
            m_Context.ReleaseByteStream(sendArgs.UserToken as ByteStream);
            lock (m_ArgPool)
            {
                sendArgs.SetBuffer(null, 0, 0);
                m_ArgPool.Release(sendArgs);
            }

            if (sendArgs.SocketError != SocketError.Success)
            {
                Debug.LogError("DataBlock send failed!");
                CloseSocket(SocketShutdown.Send);
            }
        }

        private void ResetArgs(SocketAsyncEventArgs args)
        {
            args.Completed -= OnIOCompleted;
            if (args.AcceptSocket != null)
            {
                args.AcceptSocket.Close();
                args.AcceptSocket = null;
            }
            args.UserToken = null;
            args.SetBuffer(null, 0, 0);
        }

        private SocketAsyncEventArgs GetSendArgs(ByteStream stream)
        {
            lock (m_ArgPool)
            {
                var args = m_ArgPool.Get();
                if (args.UserToken == null)
                {
                    args.Completed -= OnIOCompleted;
                    args.Completed += OnIOCompleted;
                }

                args.SetBuffer(stream.Buffer, 0, (int)stream.Length);
                args.UserToken = stream;

                return args;
            }
        }

        private void CloseSocket(SocketShutdown shutdown)
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

        private DataBlock m_Block;
        private readonly Socket m_Socket;
        private readonly INetworkContext m_Context;
        private Action<IDataBlock> m_DataReceivedCallback;
        private readonly IObjectPool<SocketAsyncEventArgs> m_ArgPool;
    }
}

//XDay