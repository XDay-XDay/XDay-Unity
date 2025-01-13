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

namespace XDay.NetworkAPI
{
    internal class DumbHandShaker : IHandShaker
    {
        public DumbHandShaker(INetworkContext context)
        {
            m_Context = context;
        }

        public void OnDestroy(bool closeSocket)
        {
        }

        public void Start(IConnection connection, Action<IMessagePipeline> onHandShakeComplete)
        {
            var qualityStage = m_Context.Config.CreateQualityStage(connection.Transporter, m_Context.Name);
            var flowControlStage = m_Context.Config.CreateFlowControlStage(qualityStage, m_Context);
            var compressionStage = new CompressionStageForward();
            var encryptionStage = new EncryptionStageForward();
            var pipeline = new MessagePipeline(
                m_Context,
                m_Context.ProtocalStage,
                compressionStage,
                encryptionStage,
                flowControlStage,
                qualityStage);

            onHandShakeComplete(pipeline);
        }

        public TransporterState Update()
        {
            return TransporterState.Done;
        }

        private readonly INetworkContext m_Context;
    }

    internal class DumbHandShakerCreator : IHandShakerCreator
    {
        public IHandShaker Create(Socket socket, INetworkContext controller)
        {
            return new DumbHandShaker(controller);
        }
    }
}


//XDay