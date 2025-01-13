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
    internal partial class ClientConnectionTCP
    {
        private class StateHandShake : State
        {
            public StateHandShake(ClientConnectionTCP connection, IHandShaker handShaker, Action<IConnection> onConnectEstablished)
            {
                m_IsConnecting = true;
                m_Connection = connection;
                m_HandShaker = handShaker;
                m_OnConnectionEstablished = onConnectEstablished;
            }

            public override void OnEnter()
            {
                m_HandShaker.Start(m_Connection, (pipeline) =>
                {
                    m_IsConnecting = false;
                    m_Connection.m_MessagePipeline = pipeline;
                    m_Connection.SetState(m_Connection.m_TransportState);
                    m_OnConnectionEstablished?.Invoke(m_Connection);
                }
                );
            }

            public override TransporterState Update()
            {
                if (m_HandShaker.Update() == TransporterState.Done)
                {
                    m_HandShaker.OnDestroy(false);
                    m_HandShaker = null;

                    if (m_IsConnecting)
                    {
                        return TransporterState.Done;
                    }
                }
                return TransporterState.Running;
            }

            private bool m_IsConnecting;
            private IHandShaker m_HandShaker;
            private readonly Action<IConnection> m_OnConnectionEstablished;
            private readonly ClientConnectionTCP m_Connection;
        }
    }
}

//XDay