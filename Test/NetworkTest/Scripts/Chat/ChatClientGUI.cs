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
using TMPro;
using ProtoBuf;

internal class ChatClientGUI : MonoBehaviour, IChatClientUserInterface
{
    public void Init(ChatClient client)
    {
        m_Client = client;
        m_LoginButton = gameObject.transform.Find("Login").GetComponent<Button>();
        m_LoginButton.onClick.AddListener(OnClickLogin);

        m_SendButton = gameObject.transform.Find("Send").GetComponent<Button>();
        m_SendButton.onClick.AddListener(OnClickSend);

        m_LoginUserName = gameObject.transform.Find("UserName/Text Area/Text").GetComponent<TextMeshProUGUI>();
        m_MessageInputText = gameObject.transform.Find("Input/Text Area/Text").GetComponent<TextMeshProUGUI>();
        m_MessageInputGameObject = gameObject.transform.Find("Input").gameObject;
        m_UserInputGameObject = gameObject.transform.Find("UserName").gameObject;
        m_ScrollViewContentGameObject = gameObject.transform.Find("Scroll View/Viewport/Content").gameObject;
        m_ScrollViewGameObject = gameObject.transform.Find("Scroll View").gameObject;
        m_MessageGameObject = gameObject.transform.Find("Message").gameObject;

        m_MessageInputGameObject.SetActive(false);
        m_MessageGameObject.SetActive(false);
        m_SendButton.gameObject.SetActive(false);
        m_ScrollViewGameObject.SetActive(false);
    }

    public void OnGetLoginResult(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            m_LoginButton.gameObject.SetActive(false);
            m_UserInputGameObject.SetActive(false);
            m_MessageInputGameObject.SetActive(true);
            m_SendButton.gameObject.SetActive(true);
            m_ScrollViewGameObject.SetActive(true);
        }
    }

    public void OnAddMessage(UserMessage message)
    {
        var obj = Instantiate(m_MessageGameObject);
        obj.SetActive(true);
        obj.transform.SetParent(m_ScrollViewContentGameObject.transform);
    }

    private bool ValidateLogin()
    {
        if (m_LoginUserName.text.Length > 1)
        {
            return true;
        }
        return false;
    }

    private void OnClickLogin()
    {
        bool ok = ValidateLogin();
        if (ok)
        {
            m_Client.Login(m_LoginUserName.text);
        }
        else
        {
            Debug.LogError("Input user name");
        }
    }

    private void OnClickSend()
    {
        var req = new MessageReq
        {
            Text = m_MessageInputText.text
        };
        m_Client.Send(req);
    }

    private Button m_LoginButton;
    private Button m_SendButton;
    private TextMeshProUGUI m_LoginUserName;
    private TextMeshProUGUI m_MessageInputText;
    private GameObject m_MessageInputGameObject;
    private GameObject m_UserInputGameObject;
    private GameObject m_ScrollViewContentGameObject;
    private GameObject m_ScrollViewGameObject;
    private GameObject m_MessageGameObject;
    private ChatClient m_Client;
}

//XDay