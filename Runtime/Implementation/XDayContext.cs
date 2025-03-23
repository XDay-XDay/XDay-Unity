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
using XDay.CameraAPI;
using XDay.InputAPI;
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI;
using XDay.AssetAPI;
using XDay.GUIAPI;
using System;

namespace XDay.API
{
    internal class XDayContext : IXDayContext
    {
        public IDeviceInput DeviceInput => m_Input;
        public IWorldManager WorldManager => m_WorldSystem;
        public ITaskSystem TaskSystem => m_TaskSystem;
        public INavigationManager NavigationManager => m_NavigationManager;
        public IAssetLoader WorldAssetLoader => m_WorldSystem?.WorldAssetLoader;
        public IUIWindowManager WindowManager => m_WindowManager;
        public ITickTimer TickTimer => m_TickTimer;
        public ITickTimer FrameTimer => m_FrameTimer;
        public IEventSystem EventSystem => m_EventSystem;

        public XDayContext(string worldSetupFilePath, IAssetLoader loader, bool enableLog, bool enableUI)
        {
            Debug.Assert(!string.IsNullOrEmpty(worldSetupFilePath));
            Debug.Assert(loader != null);

            var logSetting = new LogSetting()
            {
                LogName = "Client",
                ActionOnInit = () => {
                    Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
                },
                ActionOnDestroy = () => {
                    Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
                },
                LogFileDirectory = Application.persistentDataPath,
            };
            Log.Init(logSetting);

            var createInfo = new TaskSystemCreateInfo();
            createInfo.LayerInfo.Add(new TaskLayerInfo(1, 1));
            m_TaskSystem = ITaskSystem.Create(createInfo);
            m_Input = IDeviceInput.Create();
            m_WorldSystem = new WorldManager();
            m_AssetLoader = loader;

            m_WorldSystem.Init(worldSetupFilePath, m_AssetLoader, m_TaskSystem, m_Input);
            m_NavigationManager = INavigationManager.Create();
            if (enableUI)
            {
                m_WindowManager = IUIWindowManager.Create(loader);
            }
            m_TickTimer = ITickTimer.Create(false, 0, () => {
                return (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
            });
            m_FrameTimer = ITickTimer.Create(false, 0, () => {
                return Time.frameCount;
            });

            m_EventSystem = IEventSystem.Create();
        }

        public void OnDestroy()
        {
            m_WindowManager?.OnDestroy();
            m_WorldSystem?.OnDestroy();
            m_NavigationManager?.OnDestroy();
            Log.Uninit();
        }

        public void Update()
        {
            m_TickTimer.Update();
            m_FrameTimer.Update();
            m_Input.Update();
            m_WindowManager?.Update(Time.deltaTime);
            m_WorldSystem?.Update();
        }

        public void LateUpdate()
        {
            m_WorldSystem?.LateUpdate();
        }

        public ICameraManipulator CreateCameraManipulator(string configPath, Camera camera)
        {
            var text = m_AssetLoader.LoadText(configPath);
            var name = Helper.GetPathName(text, false);
            var setup = new CameraSetup(name);
            setup.Load(text);

            var manipulator = ICameraManipulator.Create(camera, setup, m_Input);
            return manipulator;
        }

        public IGridBasedPathFinder CreateGridBasedPathFinder(IGridData gridData, int neighbourCount = 8)
        {
            return m_NavigationManager.CreateGridPathFinder(m_TaskSystem, gridData, neighbourCount);
        }

        private void OnUnityLogMessageReceived(string message, string stackTrace, UnityEngine.LogType type)
        {
            Log.Instance?.LogMessage(message, stackTrace, ToUnityLogType(type));
        }

        private LogType ToUnityLogType(UnityEngine.LogType type)
        {
            if (type == UnityEngine.LogType.Log)
            {
                return LogType.Log;
            }

            if (type == UnityEngine.LogType.Error)
            {
                return LogType.Error;
            }

            if (type == UnityEngine.LogType.Warning)
            {
                return LogType.Warning;
            }

            if (type == UnityEngine.LogType.Assert)
            {
                return LogType.Assert;
            }

            if (type == UnityEngine.LogType.Exception)
            {
                return LogType.Exception;
            }

            Debug.Assert(false);
            return LogType.Exception;
        }

        private readonly IAssetLoader m_AssetLoader;
        private readonly WorldManager m_WorldSystem;
        private readonly IDeviceInput m_Input;
        private readonly ITaskSystem m_TaskSystem;
        private readonly INavigationManager m_NavigationManager;
        private readonly IUIWindowManager m_WindowManager;
        private readonly ITickTimer m_TickTimer;
        private readonly ITickTimer m_FrameTimer;
        private readonly IEventSystem m_EventSystem;
    }
}


//XDay