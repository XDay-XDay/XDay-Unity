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
using XDay.GUIAPI;

class IconData
{
    public string text;
}

class TestUIData
{
    public IconData icon0;
    public IconData icon1;
}

class MyWindow : UIWindow<TestUIView, TestUIController>
{
    public override bool CacheWhenClose => false;
}

public class UITestEntry : MonoBehaviour
{
    void Start()
    {
        m_Loader = new GameAssetLoader();

        m_UIManager = IUIWindowManager.Create(m_Loader, new GameObject("UI Root"));

        OpenWindow();   
    }

    private void OnDestroy()
    {
        m_UIManager.OnDestroy();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (m_TestUIWindow == null)
            {
                OpenWindow();
            }
            else
            {
                m_UIManager.Close(m_TestUIWindow);
                m_TestUIWindow = null;
            }
        }
    }

    private void OpenWindow()
    {
        m_TestUIWindow = m_UIManager.Open<MyWindow>();
        m_TestUIWindow.SetData(new TestUIData()
        {
            icon0 = new IconData() { text = "1234" },
            icon1 = new IconData() { text = "456t" },
        });
    }

    private GameAssetLoader m_Loader;
    private IUIWindowManager m_UIManager;
    private UIWindowBase m_TestUIWindow;
}
