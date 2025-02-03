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
using XDay.API;

namespace XDay.TestGame
{
    internal class Entrance : MonoBehaviour
    {
        private void Start()
        {
            Application.runInBackground = true;
            Global.XDayContext = IXDayContext.Create("Assets/Resource/World/WorldSetupManager.asset", new GameAssetLoader(), true);
            Global.StateManager = IStateManager.Create();
            var stateMainMenu = Global.StateManager.CreateState<StateMainMenu>();
            var stateLoading = Global.StateManager.CreateState<StateLoadingGame>();
            var stateGame = Global.StateManager.CreateState<StateGame>();
            Global.StateManager.PushState<StateMainMenu>();
        }

        private void OnDestroy()
        {
            Global.XDayContext.OnDestroy();
            Global.XDayContext = null;
        }

        private void Update()
        {
            Global.XDayContext.Update();
            Global.StateManager.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            Global.XDayContext.LateUpdate();
        }
    }
}
