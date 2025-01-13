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
using System.Collections.Generic;
using XDay.NetworkAPI;

internal class ChatServer
{
    public ChatServer()
    {
        m_ServerFacade = IServerNetwork.Create(
            INetworkConfig.Create(new ProtocalStageProtoBufCreator(), 
            null, 
            new HandShakerServerCreator(),
            new CompressionStageDeflateCreator(8),
            new EncryptionStageAESCreator()));

        RegisterMessages();
    }

    public void OnDestroy()
    {
        m_ServerFacade.OnDestroy();
    }

    public void StartServer()
    {
        m_ServerFacade.StartServer(12345);
    }

    public void StopServer()
    {
        m_ServerFacade.StopServer();
    }

    public void Update()
    {
        m_ServerFacade.Update(Time.deltaTime);
    }

    private void OnLoginReq(IConnection connection, LoginReq req)
    {
        int userID = ++m_NextUserID;
        var user = new ServerUser(userID, req.UserName, connection);
        m_ServerFacade.AddUser(user);

        m_ConnectionToUserID[connection] = userID;

        LoginAck ack = new()
        {
            UserID = userID,
            Result = ""
        };

        connection.Send(ack);

        var notify = new LoginNotify
        {
            UserID = userID,
            UserName = req.UserName
        };
        m_ServerFacade.Broadcast(notify, null);
    }

    private void OnMessageReq(IConnection connection, MessageReq req)
    {
        Debug.Log($"OnMessageReq: {req.Text}");

        m_ConnectionToUserID.TryGetValue(connection, out int userID);
        User user = m_ServerFacade.GetUser(userID);

        UserMessage userMessage = new UserMessage
        {
            userID = userID,
            userName = user.Name,
            content = req.Text
        };

        AddUserMessage(userMessage);

        MessageNotify notify = new MessageNotify
        {
            Text = req.Text,
            UserID = userID
        };

        m_ServerFacade.Broadcast(notify, null);
    }

    private void AddUserMessage(UserMessage msg)
    {
        m_Messages.Add(msg);
    }

    private void RegisterMessages()
    {
        m_ServerFacade.RegisterProtocal<LoginAck>();
        m_ServerFacade.RegisterProtocal<LoginReq>();
        m_ServerFacade.RegisterProtocal<LoginNotify>();
        m_ServerFacade.RegisterProtocal<LogoutNotify>();
        m_ServerFacade.RegisterProtocal<MessageReq>();
        m_ServerFacade.RegisterProtocal<MessageNotify>();
        m_ServerFacade.RegisterProtocalHandler<LoginReq>(OnLoginReq);
        m_ServerFacade.RegisterProtocalHandler<MessageReq>(OnMessageReq);
    }

    private int m_NextUserID = 0;
    private readonly IServerNetwork m_ServerFacade;
    private readonly Dictionary<IConnection, int> m_ConnectionToUserID = new();
    private readonly List<UserMessage> m_Messages = new();
}

//XDay