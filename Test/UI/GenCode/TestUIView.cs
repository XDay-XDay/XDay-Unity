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



//this file is auto generated from UIBinder

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using XDay.GUIAPI;


public interface ITestUIViewEventHandler
{
    void OnIconClick(PointerEventData pointerData);
    void OnIconUp(PointerEventData pointerData);
    void OnIcon1Enter(PointerEventData pointerData);

}


internal class TestUIView : UIView
{
    public RawImage RawImage => m_RawImage;
    public IconView IconView => m_IconView;
    public RectTransform RectTransform => m_RectTransform;
    public GameObject Panel => m_Panel;
    public IconView IconView1 => m_IconView1;


    public TestUIView()
    {
    }

    public TestUIView(GameObject root) : base(root)
    {
    }

    public override string GetPath()
    {
        return "Assets/XDay/Test/UI/TestUI.prefab";
    }

    protected override void OnLoad()
    {
        var gameObject0 = QueryGameObject("Icon");
        var gameObject0Listener = gameObject0.AddComponent<UIEventListener>();
        gameObject0Listener.AddClickEvent(OnIconClick);
        gameObject0Listener.AddUpEvent(OnIconUp);
        var gameObject1 = QueryGameObject("Icon1");
        var gameObject1Listener = gameObject1.AddComponent<UIEventListener>();
        gameObject1Listener.AddEnterEvent(OnIcon1Enter);


        m_RawImage = QueryComponent<RawImage>("Icon", 2);
        Debug.Assert(m_RawImage != null, "m_RawImage is null");
        m_IconView = new IconView(QueryGameObject("Icon"));
        m_IconView.SetController(new IconController(m_IconView));
        m_RectTransform = QueryComponent<RectTransform>("Panel", 0);
        Debug.Assert(m_RectTransform != null, "m_RectTransform is null");
        m_Panel = QueryGameObject("Panel");
        Debug.Assert(m_Panel != null, "m_Panel is null");
        m_IconView1 = new IconView(QueryGameObject("Icon1"));
        m_IconView1.SetController(new IconController(m_IconView1));

    }

    protected override void OnShow()
    {
        m_IconView.Show();
        m_IconView1.Show();

    }

    protected override void OnHide()
    {
        m_IconView.Hide();
        m_IconView1.Hide();

    }

    protected override void OnDestroyInternal()
    {
        m_IconView.OnDestroy();
        m_IconView1.OnDestroy();

    }

    private RawImage m_RawImage;
    private IconView m_IconView;
    private RectTransform m_RectTransform;
    private GameObject m_Panel;
    private IconView m_IconView1;



    private void OnIconClick(PointerEventData pointerData)
    {
        var eventHandler = m_Controller as ITestUIViewEventHandler;
        eventHandler.OnIconClick(pointerData);
    }


    private void OnIconUp(PointerEventData pointerData)
    {
        var eventHandler = m_Controller as ITestUIViewEventHandler;
        eventHandler.OnIconUp(pointerData);
    }


    private void OnIcon1Enter(PointerEventData pointerData)
    {
        var eventHandler = m_Controller as ITestUIViewEventHandler;
        eventHandler.OnIcon1Enter(pointerData);
    }


}
