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

class ChatClientCommandLineUI : IChatClientUserInterface
{
    public void Init(ChatClient client)
    {
        m_Client = client;
    }

    public void OnAddMessage(UserMessage message)
    {
        Debug.Log($"Client Received Message: user: {message.userName}, text: {message.content}");
    }

    public void OnGetLoginResult(string status)
    {
        if (!string.IsNullOrEmpty(status))
        {
            //login succeed
            Debug.LogError($"login failed");
        }
        else
        {
            Debug.Log("login succeeded");

            for (int i = 0; i < 500; ++i)
            {
                byte[] buffer = new byte[10000];
                for (int k = 0; k < buffer.Length; ++k)
                {
                    buffer[k] = (byte)Random.Range(97, 97 + 25);
                }
                var req = new MessageReq
                {
                    //req.Text = System.Text.Encoding.UTF8.GetString(buffer);
                    Text = $"this is a very large text ya ----------- {i}"
                };
                m_Client.Send(req);
            }
        }
    }

    private ChatClient m_Client;
}


//XDay