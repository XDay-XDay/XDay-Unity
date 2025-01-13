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
using System.Security.Cryptography;
using UnityEngine;
using XDay.NetworkAPI;


/// <summary>
/// exchange AES key with server, follow steps below:
/// 1.client generate temp AES key using RSA public key from server
/// 2.client send temp AES key to server  
/// 3.server get temp AES key from client by decryption using server RSA private key
/// 4.server create AES key and encrypt it usng temp AES key, then send to client
/// 5.client decrypt to get AES key
/// client do steps 3,4
/// </summary>
internal class HandShakerServer : IHandShaker
{
    public HandShakerServer(Socket socket, INetworkContext controller)
    {
        m_Socket = socket;
        m_Controller = controller;
        m_Transporter = new BlockTransporter(m_Socket, m_Controller);
    }

    public void Start(IConnection connection,Action<IMessagePipeline> onHandShakeComplete)
    {
        m_OnHandShakeComplete = onHandShakeComplete;
        m_Connection = connection;
        m_IsReadingHeader = true;
        m_Transporter.DataReceivedCallback = OnBlockReceived;
        m_Block = m_Transporter.Receive(4);
    }

    public void OnDestroy(bool closeSocket)
    {
        m_Transporter.OnDestroy(closeSocket);
    }

    public TransporterState Update()
    {
        return m_Transporter.Update();
    }

    private IMessagePipeline CreateMessagePipeline(byte[] key)
    {
        var qualityStage = m_Controller.Config.CreateQualityStage(m_Connection.Transporter, m_Controller.Name);
        var flowControlStage = m_Controller.Config.CreateFlowControlStage(qualityStage, m_Controller);
        var compressionStage = m_Controller.Config.CreateCompressionStage();
        var encryptionStage = m_Controller.Config.CreateEncryptionStage(key);
        return IMessagePipeline.Create(
            m_Controller,
            m_Controller.ProtocalStage,
            compressionStage,
            encryptionStage,
            flowControlStage,
            qualityStage);
    }

    private void OnBlockReceived(IDataBlock block)
    {
        if (m_IsReadingHeader)
        {
            //read header
            m_IsReadingHeader = false;
            int bodySize = NetHelper.BytesToInt32(block.Buffer, 0);
            m_Block.SetSize(bodySize);
        }
        else
        {
            //read keys
            RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(RSAKey);
            byte[] tempKey = rsa.Decrypt(block.Buffer, true);

            var finalKey = Helper.GenerateAESKey();

            var transformer = new AESTransformer(tempKey, tempKey);
            ByteStream output = m_Controller.GetByteStream();

            //write length
            output.WriteInt32(finalKey.Length);
            output.SetLength(output.Length + finalKey.Length);
            int n = transformer.TransformData(finalKey, finalKey.Length, output, outputOffset: (int)output.Position);
            if (n != Helper.AESKeyLength)
            {
                Debug.LogError(string.Format("Invalid AES key length", n));
            }
            else
            {
                m_Transporter.Send(output);
                m_OnHandShakeComplete(CreateMessagePipeline(finalKey));
            }
        }
    }

    private readonly BlockTransporter m_Transporter;
    private readonly Socket m_Socket;
    private readonly INetworkContext m_Controller;
    private IConnection m_Connection;
    private Action<IMessagePipeline> m_OnHandShakeComplete;
    private IDataBlock m_Block;
    private bool m_IsReadingHeader;
    private const string RSAKey = "<RSAKeyValue><Modulus>hVSS9IZt3kEtjCXfmcLqaNmcGAR7zGt+ZAv5ZO8sqCGbT53mYyH7H5MO1PJVXqZ+gDnVYqwniouZ2JTFiJN21QxNeXwrU4mZweEpEzuwCG2L8FQLfVLQ+ZHPvPrpR+h5tOSqa85LIWJgSocqi6KfJlGYOGBjg4z9xOIfP+PtiXU=</Modulus><Exponent>AQAB</Exponent><P>2Ulr/oQtynfEC7hOhpVZ9n3cM94hm3RgDEwrE4wmTug8J8tsPUBRZES/7bIPMnqPJPN0+3A9zIt89qB4kC5WFw==</P><Q>nRXZm+sIvDfG10HtDMd8kF48cAOXBVoqP3TxwHddaTKGzbLjxokOOzhfUZTAm6q5+KZsqenFPT/M0MiHorlgUw==</Q><DP>efrc1IHxjuMTPJ0YADehzF21m6yM408+iEjOOegIrW10L8bkGbKcvpRVxqOaInVpHI5L0sec+dIose8+H3rTuw==</DP><DQ>F6yUWDhK37r0P7rS1As4jbV2HFeeKhNVrKyeRqh2roUL5fJg+6nqOCidzPjDMnK/hmbml5EAxeNYpdqi/nY7uw==</DQ><InverseQ>veS6XGANP8+8YIlPQzZtG+dnsZ70Y5EHUr+i04YLLpZAVTZC3rQhP+Stmoft9CCf2nwr4rD+kljf/XwNs70qTg==</InverseQ><D>JIjGM+baGDq35l1CZfm5Db4DPbmMyrjxyyxUzEmVbQS4cBqOVL+s9jvvpn440lTA+RXf2MffleEm6OfrML9nohbFTMYgK7ruvt7rqwBeu5/As1N8Pn+glykVPOEruJSgj56/8WWOJK/oW6W5S3Cva+OhX9akhehtXro1EkLJm8k=</D></RSAKeyValue>";
}

public class HandShakerServerCreator : IHandShakerCreator
{
    public IHandShaker Create(Socket socket, INetworkContext context)
    {
        return new HandShakerServer(socket, context);
    }
}
