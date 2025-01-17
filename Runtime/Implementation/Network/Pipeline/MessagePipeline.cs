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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NetworkAPI
{
    internal partial class MessagePipeline : IMessagePipeline
    {
        public MessagePipeline(
            INetworkContext context,
            IProtocalStage protocalStage,
            ICompressionStage compressionStage,
            IEncryptionStage encryptionStage,
            IFlowControlStage flowControlStage,
            IQualityStage qualityStage)
        {
            m_Context = context;
            m_ProtocalStage = protocalStage;
            m_CompressionStage = compressionStage;
            m_EncryptionStage = encryptionStage;
            m_FlowControlStage = flowControlStage;
            m_QualityStage = qualityStage;
            m_BlockReader = new BlockReader(4, m_Context.ArrayPool);
        }

        public void Send(object protocal)
        {
            var stream = m_Context.GetByteStream();
            m_ProtocalStage.Encode(protocal, stream);
            stream.Position = 0;

            var compressedStream = m_Context.GetByteStream();
            m_CompressionStage.Compress(stream, compressedStream);
            m_Context.ReleaseByteStream(stream);
            stream = compressedStream;

            var encryptedStream = m_Context.GetByteStream();
            m_EncryptionStage.Encrypt(stream, encryptedStream);
            m_Context.ReleaseByteStream(stream);

            var finalStream = m_Context.GetByteStream();
            finalStream.WriteInt32((int)encryptedStream.Length);
            finalStream.Write(encryptedStream.Buffer, 0, (int)encryptedStream.Length);
            m_Context.ReleaseByteStream(encryptedStream);

            if (!m_FlowControlStage.CheckOverflow(finalStream))
            {
                m_QualityStage.Send(finalStream);
            }
        }

        public void Receive(
            byte[] buffer,
            int offset,
            int count,
            Action<object> onProtocalReceived)
        {
            while (true)
            {
                var n = m_BlockReader.Read(buffer, offset, count);
                offset += n;
                count -= n;

                if (m_BlockReader.Full)
                {
                    if (m_IsReadingHeader)
                    {
                        m_IsReadingHeader = false;
                        m_BlockReader.SetSize(Helper.BytesToInt32(m_BlockReader.Buffer, 0));
                    }
                    else
                    {
                        m_IsReadingHeader = true;
                        onProtocalReceived(DecodeProtocal(m_BlockReader.Buffer));
                        m_BlockReader.SetSize(4);
                    }
                }

                if (count == 0)
                {
                    break;
                }
            }
        }

        public void OnDestroy()
        {
            m_BlockReader?.OnDestroy();
            m_ProtocalStage?.OnDestroy();
            m_FlowControlStage?.OnDestroy();
            m_QualityStage?.OnDestroy();
        }

        public void Update()
        {
            m_FlowControlStage.Update();
            m_QualityStage.Update();
        }

        private object DecodeProtocal(byte[] buffer)
        {
            try
            {
                var encryptedStream = m_Context.GetByteStream();
                encryptedStream.Write(buffer);

                var compressedStream = m_Context.GetByteStream();
                m_EncryptionStage.Decrypt(encryptedStream, compressedStream);
                m_Context.ReleaseByteStream(encryptedStream);

                var protocalStream = compressedStream;
                if (m_CompressionStage != null)
                {
                    protocalStream = m_Context.GetByteStream();
                    m_CompressionStage.Decompress(compressedStream, protocalStream);
                    m_Context.ReleaseByteStream(compressedStream);
                }

                var protocal = m_ProtocalStage.Decode(protocalStream);
                m_Context.ReleaseByteStream(protocalStream);

                return protocal;
            }
            catch (Exception e)
            {
                Debug.LogError($"{m_Context.Name}:{e}");
            }

            return null;
        }

        private readonly IEncryptionStage m_EncryptionStage;
        private readonly IQualityStage m_QualityStage;
        private readonly ICompressionStage m_CompressionStage;
        private readonly IFlowControlStage m_FlowControlStage;
        private readonly IProtocalStage m_ProtocalStage;
        private readonly INetworkContext m_Context;
        private readonly BlockReader m_BlockReader;
        private bool m_IsReadingHeader = true;
    }
}


//XDay