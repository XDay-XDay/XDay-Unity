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

using ProtoBuf;
using UnityEngine;
using XDay.NetworkAPI;

internal class NetworkTest : MonoBehaviour
{
    private void Start()
    {
        m_Context = INetworkContext.Create("client/server", INetworkConfig.Create(new ProtocalStageProtoBufCreator()));

        RegisterMessages();

        StartServer();

        StartClient();
    }

    private void RegisterMessages()
    {
        m_Context.RegisterProtocal<LoginReq>();
    }

    private void StartServer()
    {
        m_Server = m_Context.CreateServer(true, null);
        m_Server.Start(m_ServerPort);
    }

    private void StartClient()
    {
        m_Client = m_Context.CreateClient(true, null);
        m_Client.Connect("127.0.0.1", m_ServerPort, OnConnected, out _);
    }

    private void OnConnected(IConnection connection)
    {
        m_Connection = connection;
        if (connection != null)
        {
            Debug.Log("Connect to server successfully!");

            LoginReq person = new()
            {
                UserName = "wzw"
            };
            m_Connection.Send(person);
        }
        else
        {
            Debug.LogError("Connect to server failed!");
        }
    }

    private void Update()
    {
        m_Client.Update();
        m_Server.Update();
    }

    private INetworkContext m_Context;
    private readonly int m_ServerPort = 12345;
    private IConnection m_Connection;
    private IClient m_Client;
    private IServer m_Server;
}

