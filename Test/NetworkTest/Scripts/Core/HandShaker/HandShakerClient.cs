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
/// client do steps 1,2,5
/// </summary>
internal class HandShakerClient : IHandShaker
{
    public HandShakerClient(Socket socket, INetworkContext controller)
    {
        m_Socket = socket;
        m_Controller = controller;
        m_Transporter = new BlockTransporter(m_Socket, m_Controller);
    }

    public void Start(IConnection connection, Action<IMessagePipeline> onHandShakeComplete)
    {
        m_Connection = connection;
        m_OnHandShakeComplete = onHandShakeComplete;

        m_TempKey = Helper.GenerateAESKey();
        RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(RSAPublicKey);
        byte[] result = rsa.Encrypt(m_TempKey, true);
        ByteStream stream = m_Controller.GetByteStream();
        stream.WriteInt32(result.Length);
        stream.Write(result);
        m_Transporter.Send(stream);

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
            var transformer = new AESTransformer(m_TempKey, m_TempKey);
            ByteStream output = m_Controller.GetByteStream();
            output.SetLength(block.Size);
            int n = transformer.TransformData(block.Buffer, block.Size, output, outputOffset: 0);
            if (n != Helper.AESKeyLength)
            {
                Debug.LogError(string.Format("Invalid AES key length", n));
            }
            else
            {
                byte[] finalKey = new byte[n];
                Array.Copy(output.Buffer, finalKey, finalKey.Length);
                m_OnHandShakeComplete(CreateMessagePipeline(finalKey));
            }
            m_Controller.ReleaseByteStream(output);
        }
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

    private readonly BlockTransporter m_Transporter;
    private readonly Socket m_Socket;
    private readonly INetworkContext m_Controller;
    private IConnection m_Connection;
    private Action<IMessagePipeline> m_OnHandShakeComplete;
    private IDataBlock m_Block;
    private byte[] m_TempKey;
    private bool m_IsReadingHeader;
    private const string RSAPublicKey = "<RSAKeyValue><Modulus>hVSS9IZt3kEtjCXfmcLqaNmcGAR7zGt+ZAv5ZO8sqCGbT53mYyH7H5MO1PJVXqZ+gDnVYqwniouZ2JTFiJN21QxNeXwrU4mZweEpEzuwCG2L8FQLfVLQ+ZHPvPrpR+h5tOSqa85LIWJgSocqi6KfJlGYOGBjg4z9xOIfP+PtiXU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
}

public class HandShakerClientCreator : IHandShakerCreator
{
    public IHandShaker Create(Socket socket, INetworkContext context)
    {
        return new HandShakerClient(socket, context);
    }
}

//XDay