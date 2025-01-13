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

namespace XDay.NetworkAPI
{
    public interface INetworkConfig
    {
        public static INetworkConfig Create(
            IProtocalStageCreator protocalStageCreator,
            ITransportStageCreator transportStageCreator = null,
            IHandShakerCreator handShakerCreator = null,
            ICompressionStageCreator compressionStageCreator = null,
            IEncryptionStageCreator encryptionStageCreator = null,
            IFlowControlStageCreator flowControlStageCreator = null,
            IQualityStageCreator qualityStageCreator = null)
        {
            return new NetworkConfig(
                protocalStageCreator,
                transportStageCreator,
                compressionStageCreator, 
                encryptionStageCreator, 
                flowControlStageCreator, 
                qualityStageCreator, 
                handShakerCreator);
        }

        IHandShaker CreateHandShaker(Socket socket, INetworkContext context);
        IProtocalStage CreateProtocalStage();
        ITransportStage CreateTransportStage(INetworkContext context);
        IFlowControlStage CreateFlowControlStage(ITransportable transportable, INetworkContext context);
        ICompressionStage CreateCompressionStage();
        IQualityStage CreateQualityStage(ITransportable transportable, string name);
        IEncryptionStage CreateEncryptionStage(object data);
    }
}

//XDay