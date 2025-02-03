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



internal class UILoadingView : UIView
{
    public Slider ProgressBar => m_ProgressBar;
    public TextMeshProUGUI Progress => m_Progress;


    public UILoadingView()
    {
    }

    public UILoadingView(GameObject root) : base(root)
    {
    }

    public override string GetPath()
    {
        return "Assets/XDay/Test/Game/Assets/UI/UILoading.prefab";
    }

    protected override void OnLoad()
    {


        m_ProgressBar = QueryComponent<Slider>("ProgressBar", 1);
        Debug.Assert(m_ProgressBar != null, "m_ProgressBar is null");
        m_Progress = QueryComponent<TextMeshProUGUI>("Progress", 2);
        Debug.Assert(m_Progress != null, "m_Progress is null");

    }

    protected override void OnShow()
    {

    }

    protected override void OnHide()
    {

    }

    protected override void OnDestroyInternal()
    {

    }

    private Slider m_ProgressBar;
    private TextMeshProUGUI m_Progress;



}


internal partial class UILoadingController : UIController<UILoadingView>
{
    public UILoadingController(UILoadingView view) : base(view)
    {
    }
}
