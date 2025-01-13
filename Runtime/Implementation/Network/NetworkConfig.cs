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

using System.Net.Sockets;
using UnityEngine;

namespace XDay.NetworkAPI
{
    internal class NetworkConfig : INetworkConfig
    {
        public NetworkConfig(
            IProtocalStageCreator protocalStageCreator,
            ITransportStageCreator transportStageCreator,
            ICompressionStageCreator compressionStageCreator,
            IEncryptionStageCreator encryptionStageCreator,
            IFlowControlStageCreator flowControlStageCreator,
            IQualityStageCreator qualityStageCreator,
            IHandShakerCreator handShakerCreator)
        {
            Debug.Assert(protocalStageCreator != null, "Protocal stage creator is null");

            m_ProtocalLayerCreator = protocalStageCreator;
            m_HandShakerCreator = handShakerCreator;
            m_HandShakerCreator ??= new DumbHandShakerCreator();
            m_CompressionLayerCreator = compressionStageCreator;
            m_CompressionLayerCreator ??= new CompressionLayerForwardCreator();
            m_EncryptionLayerCreator = encryptionStageCreator;
            m_EncryptionLayerCreator ??= new EncryptionLayerForwardCreator();
            m_FlowControlLayerCreator = flowControlStageCreator;
            m_FlowControlLayerCreator ??= new FlowControlStageForwardCreator();
            m_QualityLayerCreator = qualityStageCreator;
            m_QualityLayerCreator ??= new QualityStageForwardCreator();
            m_TransportLayerCreator = transportStageCreator;
            m_TransportLayerCreator ??= new TransportStageTCPCreator();
        }

        public IHandShaker CreateHandShaker(Socket socket, INetworkContext controller)
        {
            return m_HandShakerCreator.Create(socket, controller);
        }

        public IProtocalStage CreateProtocalStage()
        {
            return m_ProtocalLayerCreator.Create();
        }

        public ICompressionStage CreateCompressionStage()
        {
            return m_CompressionLayerCreator.Create();
        }

        public IEncryptionStage CreateEncryptionStage(object data)
        {
            return m_EncryptionLayerCreator.Create(data);
        }

        public IFlowControlStage CreateFlowControlStage(ITransportable transportable, INetworkContext context)
        {
            return m_FlowControlLayerCreator.Create(transportable, context);
        }

        public IQualityStage CreateQualityStage(ITransportable transportable, string name)
        {
            return m_QualityLayerCreator.Create(name, transportable);
        }

        public ITransportStage CreateTransportStage(INetworkContext context)
        {
            return m_TransportLayerCreator.Create(context);
        }

        private readonly IProtocalStageCreator m_ProtocalLayerCreator;
        private readonly ICompressionStageCreator m_CompressionLayerCreator;
        private readonly IEncryptionStageCreator m_EncryptionLayerCreator;
        private readonly IFlowControlStageCreator m_FlowControlLayerCreator;
        private readonly IQualityStageCreator m_QualityLayerCreator;
        private readonly ITransportStageCreator m_TransportLayerCreator;
        private readonly IHandShakerCreator m_HandShakerCreator;
    }
}

//XDay