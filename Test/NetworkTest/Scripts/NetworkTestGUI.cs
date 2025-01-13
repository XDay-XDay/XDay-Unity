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

using UnityEngine;
using UnityEngine.UI;
using ProtoBuf;
using TMPro;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using XDay.NetworkAPI;


class NetworkTestGUI : MonoBehaviour
{
    void Start()
    {
        m_Context = INetworkContext.Create("Client/Server", INetworkConfig.Create(new ProtocalStageProtoBufCreator()));

        m_Client = m_Context.CreateClient(true, OnClientProtocalReceived);
        m_Server = m_Context.CreateServer(true, OnServerProtocalReceived);

        RegisterMessages();

        m_ConnectButton = gameObject.transform.Find("Connect").GetComponent<Button>();
        m_ConnectButton.onClick.AddListener(() =>
        {
            Connect();
        });

        m_DisconnectButton = gameObject.transform.Find("Disconnect").GetComponent<Button>();
        m_DisconnectButton.onClick.AddListener(() =>
        {
            Disconnect();
        });

        m_StartServerButton = gameObject.transform.Find("StartServer").GetComponent<Button>();
        m_StartServerButton.onClick.AddListener(() =>
        {
            StartServer();
        });

        m_StopServerButton = gameObject.transform.Find("StopServer").GetComponent<Button>();
        m_StopServerButton.onClick.AddListener(() =>
        {
            StopServer();
        });

        m_LoginButton = gameObject.transform.Find("Login").GetComponent<Button>();
        m_LoginButton.onClick.AddListener(() =>
        {
            Login();
        });

        m_ClientConnectionCount = gameObject.transform.Find("ClientConnection").GetComponent<TextMeshProUGUI>();
        m_ServerConnectionCount = gameObject.transform.Find("ServerConnection").GetComponent<TextMeshProUGUI>();
    }

    private void RegisterMessages()
    {
        m_Context.RegisterProtocal<LoginAck>();
        m_Context.RegisterProtocal<LoginReq>();

        m_ClientProtocalHandlers[typeof(LoginAck)] = (IConnection connection, IMessage msg) =>
        {
            var ack = msg as LoginAck;
        };

        m_ServerProtocalHandlers[typeof(LoginReq)] = (IConnection connection, IMessage msg) =>
        {
            var req = msg as LoginReq;

            LoginAck ack = new()
            {
                UserID = 100
            };

            connection.Send(ack);
        };
    }

    private void Connect()
    {
        m_Client.Connect("127.0.0.1", m_ServerPort, OnConnected, out _);
    }

    private void Disconnect()
    {
        m_ClientConnection.Disconnect();
    }

    private void OnConnected(IConnection connection)
    {
        m_ClientConnection = connection;
        if (connection != null)
        {
            Debug.Log("Connect to server successfully!");
        }
        else
        {
            Debug.LogError("Connect to server failed!");
        }
    }

    private void StartServer()
    {
        m_Server.Start(m_ServerPort);
    }

    private void StopServer()
    {
        m_Server.Stop();
    }

    private void Login()
    {
        LoginReq person = new()
        {
            UserName = "xday"
        };

        m_ClientConnection?.Send(person);
    }

    private void OnClientProtocalReceived(IConnection connection, object protocal)
    {
        var prop = protocal as IMessage;
        m_ClientProtocalHandlers[prop.GetType()](connection, prop);
    }

    private void OnServerProtocalReceived(IConnection connection, object protocal)
    {
        var prop = protocal as IMessage;
        m_ServerProtocalHandlers[prop.GetType()](connection, prop);
    }

    private void Update()
    {
        m_Client.Update();
        m_Server.Update();

        m_ClientConnectionCount.text = $"Client Connection: {m_Client.ConnectionCount}";
        m_ServerConnectionCount.text = $"Server Connection: {m_Server.ConnectionCount}";
    }

    private Button m_ConnectButton;
    private Button m_DisconnectButton;
    private Button m_StartServerButton;
    private Button m_StopServerButton;
    private Button m_LoginButton;
    private TextMeshProUGUI m_ClientConnectionCount;
    private TextMeshProUGUI m_ServerConnectionCount;
    private INetworkContext m_Context;
    private IClient m_Client;
    private IServer m_Server;
    private IConnection m_ClientConnection;
    private readonly int m_ServerPort = 12345;
    private readonly Dictionary<Type, Action<IConnection, IMessage>> m_ServerProtocalHandlers = new();
    private readonly Dictionary<Type, Action<IConnection, IMessage>> m_ClientProtocalHandlers = new();
}

