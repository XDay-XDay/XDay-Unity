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
using ProtoBuf;
using System;
using System.Collections.Generic;
using XDay.NetworkAPI;


internal class ChatClient
{
    public event Action<string> EventGetLoginResult;
    public event Action<UserMessage> EventAddMessage;

    public ChatClient()
    {
        m_ClientNetwork = IClientNetwork.Create(
            INetworkConfig.Create(new ProtocalStageProtoBufCreator(),
            null, 
            new HandShakerClientCreator(), 
            new CompressionStageDeflateCreator(8), 
            new EncryptionStageAESCreator(), 
            new FlowControlStageSimpleCreator()));

        RegisterMessages();
    }

    public void OnDestroy()
    {
        m_ClientNetwork.OnDestroy();
    }

    void RegisterMessages()
    {   
        m_ClientNetwork.RegisterProtocal<LoginAck>();
        m_ClientNetwork.RegisterProtocal<LoginReq>();
        m_ClientNetwork.RegisterProtocal<LoginNotify>();
        m_ClientNetwork.RegisterProtocal<LogoutNotify>();
        m_ClientNetwork.RegisterProtocal<MessageReq>();
        m_ClientNetwork.RegisterProtocal<MessageNotify>();
        m_ClientNetwork.RegisterProtocalHandler<LoginAck>(OnLoginAck);
        m_ClientNetwork.RegisterProtocalHandler<LoginNotify>(OnLoginNotify);
        m_ClientNetwork.RegisterProtocalHandler<LogoutNotify>(OnLogoutNotify);
        m_ClientNetwork.RegisterProtocalHandler<MessageNotify>(OnMessageNotify);
    }

    public void Login(string userName)
    {
        if (m_Logged)
        {
            return;
        }
        m_Logged = true;
        m_UserName = userName;
        m_ClientNetwork.Connect("127.0.0.1", m_ServerPort, OnConnected);
    }

    public void Logout()
    {
        m_ClientNetwork.Disconnect(true);
        m_Logged = false;
    }

    public void Send(object protocal)
    {
        m_ClientNetwork.Send(protocal);
    }

    public void Update()
    {
        m_ClientNetwork.Update(Time.deltaTime);
    }

    private void OnConnected(bool ok)
    {
        if (ok)
        {
            Debug.Log("Connect to server successfully!");

            LoginReq req = new()
            {
                UserName = m_UserName
            };

            Send(req);
        }
        else
        {
            Debug.LogError("Connect to server failed, please try again!");

            m_Logged = false;
        }
    }

    private void OnLoginAck(IConnection connection, LoginAck ack)
    {
        EventGetLoginResult?.Invoke(ack.Result);
    }

    private void OnLoginNotify(IConnection connection, LoginNotify ntf)
    {
        var user = new User(ntf.UserID, ntf.UserName);
        AddUser(user);
    }

    private void OnLogoutNotify(IConnection connection, LogoutNotify notify)
    {
        RemoveUser(notify.UserID);
    }

    private void OnMessageNotify(IConnection connection, MessageNotify notify)
    {
        User user = GetUser(notify.UserID);

        var message = new UserMessage
        {
            content = notify.Text,
            userName = user.Name,
            userID = user.UserID
        };
        AddMessage(message);
    }

    private User GetUser(int userID)
    {
        m_Users.TryGetValue(userID, out var user);
        return user;
    }

    private void AddUser(User user)
    {
        m_Users.Add(user.UserID, user);
    }

    private void RemoveUser(int userID)
    {
        m_Users.Remove(userID);
    }

    private void AddMessage(UserMessage message)
    {
        m_Messages.Add(message);

        EventAddMessage?.Invoke(message);
    }

    private readonly int m_ServerPort = 12345;
    private string m_UserName;
    private bool m_Logged;
    private readonly List<UserMessage> m_Messages = new();
    private readonly Dictionary<long, User> m_Users = new();
    private readonly IClientNetwork m_ClientNetwork;
}

interface IChatClientUserInterface
{
}

//XDay