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

using System.Net;
using System.Net.Sockets;

namespace XDay.NetworkAPI
{
    interface ISocketCancellationCancelTokenManager
    {
        void RemoveToken(ISocketCancellationToken token);
    }

    internal class SocketCancellationToken : ISocketCancellationToken
    {
        public string IP => m_IP;
        public int Port => m_Port;

        public SocketCancellationToken(
            Socket socket, 
            IPEndPoint address,
            ISocketCancellationCancelTokenManager manager)
        {
            m_Socket = socket;
            m_Manager = manager;
            m_IP = address.Address.ToString();
            m_Port = address.Port;
        }

        public void Cancel()
        {
            m_Socket?.Close();
            m_Socket = null;
            m_Manager.RemoveToken(this);
        }

        private Socket m_Socket;
        private readonly string m_IP;
        private readonly int m_Port;
        private readonly ISocketCancellationCancelTokenManager m_Manager;
    }
}

//XDay