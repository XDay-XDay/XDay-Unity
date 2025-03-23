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

using XDay.CameraAPI;
using XDay.InputAPI;
using XDay.WorldAPI;
using UnityEngine;
using XDay.AssetAPI;
using XDay.GUIAPI;

namespace XDay.API
{
    /// <summary>
    /// XDay framework entrance, check WorldPreview.cs file as an example usecase
    /// </summary>
    public interface IXDayContext
    {
        static IXDayContext Create(
            string worldSetupFilePath,
            IAssetLoader loader,
            bool enableLog,
            bool enableUI)
        {
            return new XDayContext(worldSetupFilePath, loader, enableLog, enableUI);
        }

        IDeviceInput DeviceInput { get; }
        IWorldManager WorldManager { get; }
        ITaskSystem TaskSystem { get; }
        IAssetLoader WorldAssetLoader { get; }
        IUIWindowManager WindowManager { get; }
        ITickTimer TickTimer { get; }
        ITickTimer FrameTimer { get; }
        IEventSystem EventSystem { get; }

        void OnDestroy();
        void Update();
        void LateUpdate();
        ICameraManipulator CreateCameraManipulator(string configPath, Camera camera);
    }
}

//XDay