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
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal partial class ClientConnectionTCP : IConnectionTCP
    {
        public int ID => m_ID;
        public string RemoteIP => m_IP;
        public int RemotePort => m_Port;
        public ITransporter Transporter => m_TransportState.Transporter;
        public bool EnableTCPDelay { set => m_TransportState.Transporter.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, !value); }

        public ClientConnectionTCP(
            int id,
            IHandShaker handShaker,
            Socket socket, 
            Action<IConnection, object> onProtocalReceived,
            Action<IConnection> onConnectFinish,
            bool enableTCPSendDelay,
            INetworkContext context) 
        {
            socket.Blocking = true;
            
            m_ID = id;

            var ip = socket.RemoteEndPoint as IPEndPoint;
            m_IP = ip.Address.ToString();
            m_Port = ip.Port;

            m_HandShakeState = new StateHandShake(this, handShaker, onConnectFinish);
            m_TransportState = new StateTransport(this, socket, onProtocalReceived, context);
            SetState(m_HandShakeState);

            EnableTCPDelay = enableTCPSendDelay;
        }

        public void OnDestroy()
        {
            m_MessagePipeline?.OnDestroy();
            m_TransportState.OnDestroy();
            m_HandShakeState.OnDestroy();
        }

        public void Disconnect()
        {
            m_Port = 0;
            m_IP = null;
            m_TransportState.Close(SocketShutdown.Both);
        }
        
        public TransporterState Update()
        {
            return m_ActiveState.Update();
        }

        public void Send(object protocal)
        {
            if (m_ActiveState == m_TransportState)
            {
                m_TransportState.Send(protocal);
            }
            else
            {
                Debug.LogError("Send failed, not in transport state");
            }
        }

        private void SetState(State state)
        {
            if (m_ActiveState != state)
            {
                m_ActiveState?.OnExit();
                m_ActiveState = state;
                m_ActiveState?.OnEnter();
            }
        }

        private readonly int m_ID;
        private string m_IP;
        private int m_Port;
        private readonly StateHandShake m_HandShakeState;
        private readonly StateTransport m_TransportState;
        private IMessagePipeline m_MessagePipeline;
        private State m_ActiveState;

        private abstract class State
        {
            public virtual void OnDestroy() { }
            public virtual void OnEnter() { }
            public virtual void OnExit() { }
            public abstract TransporterState Update();
        }
    }
}


//XDay