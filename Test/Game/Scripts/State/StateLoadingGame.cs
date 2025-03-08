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



using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XDay.TestGame
{
    internal class StateLoadingGame : State
    {
        public override void OnEnter(object args)
        {
            var window = Global.XDayContext.WindowManager.Open<UILoadingWindow>();
            m_Controller = window.GetController<UILoadingController>();

            StartLoading().Forget();
        }

        public override void OnExit()
        {
            Global.XDayContext.WindowManager.Close<UILoadingWindow>();
        }

        public override void Update(float dt)
        {
        }

        private async UniTask StartLoading()
        {
            await Global.XDayContext.WorldManager.LoadWorldAsync("");
            m_Controller.SetProgress(0.5f);
            await UniTask.Delay(1000);
            m_Controller.SetProgress(1.0f);
            Global.StateManager.ChangeState<StateGame>();
            //Global.XDayContext.TimerManager.FrameDelay(60, () =>
            //{
            //    Global.StateManager.ChangeState<StateGame>();
            //});
        }

        private UILoadingController m_Controller;
    }
}
